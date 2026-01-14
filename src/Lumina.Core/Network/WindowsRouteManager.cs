using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Lumina.Core.Exceptions;
using Lumina.Native.Common;
using Lumina.Native.IpHelper;
using Lumina.Native.WireGuardNT;
using Microsoft.Extensions.Logging;

namespace Lumina.Core.Network;

/// <summary>
/// Windows-specific route manager using IP Helper API.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRouteManager : IRouteManager, IDisposable
{
    private readonly ILogger<WindowsRouteManager>? _logger;
    private readonly List<ManagedRoute> _managedRoutes = [];
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new Windows route manager.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public WindowsRouteManager(ILogger<WindowsRouteManager>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ManagedRoutes
    {
        get
        {
            lock (_lock)
            {
                return _managedRoutes.Select(r => r.Destination).ToList().AsReadOnly();
            }
        }
    }

    /// <inheritdoc />
    public Task AddRouteAsync(string destination, ulong interfaceLuid, uint metric = 0, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger?.LogDebug("Adding route {Destination} via interface LUID {InterfaceLuid}", destination, interfaceLuid);

        var (address, prefixLength) = ParseCidr(destination);

        IpHelperNative.InitializeIpForwardEntry(out var row);

        row.InterfaceLuid = interfaceLuid;
        row.DestinationPrefix.PrefixLength = prefixLength;
        row.DestinationPrefix.Prefix = CreateSockaddrInet(address);
        row.NextHop = CreateSockaddrInet(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            ? IPAddress.Any
            : IPAddress.IPv6Any);
        row.Metric = metric;
        row.Protocol = NL_ROUTE_PROTOCOL.Nt_Static;

        var result = IpHelperNative.CreateIpForwardEntry2(in row);

        if (result != IpHelperNative.NO_ERROR && result != IpHelperNative.ERROR_OBJECT_ALREADY_EXISTS)
        {
            _logger?.LogError("Failed to add route {Destination}: error {ErrorCode}", destination, result);
            throw new RouteConfigurationException(destination, result);
        }

        lock (_lock)
        {
            _managedRoutes.Add(new ManagedRoute(destination, interfaceLuid, row));
        }

        _logger?.LogInformation("Added route {Destination} via interface LUID {InterfaceLuid}", destination, interfaceLuid);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteRouteAsync(string destination, ulong interfaceLuid, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger?.LogDebug("Deleting route {Destination}", destination);

        var (address, prefixLength) = ParseCidr(destination);

        IpHelperNative.InitializeIpForwardEntry(out var row);

        row.InterfaceLuid = interfaceLuid;
        row.DestinationPrefix.PrefixLength = prefixLength;
        row.DestinationPrefix.Prefix = CreateSockaddrInet(address);
        row.NextHop = CreateSockaddrInet(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            ? IPAddress.Any
            : IPAddress.IPv6Any);

        var result = IpHelperNative.DeleteIpForwardEntry2(in row);

        if (result != IpHelperNative.NO_ERROR && result != IpHelperNative.ERROR_NOT_FOUND)
        {
            _logger?.LogWarning("Failed to delete route {Destination}: error {ErrorCode}", destination, result);
        }

        lock (_lock)
        {
            _managedRoutes.RemoveAll(r => r.Destination == destination && r.InterfaceLuid == interfaceLuid);
        }

        _logger?.LogInformation("Deleted route {Destination}", destination);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task AddRoutesForAllowedIpsAsync(string[] allowedIps, ulong interfaceLuid, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var ip in allowedIps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await AddRouteAsync(ip, interfaceLuid, 0, cancellationToken);
            }
            catch (RouteConfigurationException ex)
            {
                _logger?.LogError(ex, "Failed to add route for {AllowedIp}", ip);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task RemoveAllManagedRoutesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        List<ManagedRoute> routesToRemove;

        lock (_lock)
        {
            routesToRemove = [.. _managedRoutes];
        }

        foreach (var route in routesToRemove)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await DeleteRouteAsync(route.Destination, route.InterfaceLuid, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to remove route {Destination}", route.Destination);
            }
        }
    }

    /// <summary>
    /// Disposes the route manager and removes all managed routes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            RemoveAllManagedRoutesAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error removing routes during disposal");
        }
    }

    private static (IPAddress Address, byte PrefixLength) ParseCidr(string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid CIDR notation: {cidr}", nameof(cidr));

        if (!IPAddress.TryParse(parts[0], out var address))
            throw new ArgumentException($"Invalid IP address in CIDR: {cidr}", nameof(cidr));

        if (!byte.TryParse(parts[1], out var prefix))
            throw new ArgumentException($"Invalid prefix length in CIDR: {cidr}", nameof(cidr));

        return (address, prefix);
    }

    private static SOCKADDR_INET CreateSockaddrInet(IPAddress address)
    {
        var result = new SOCKADDR_INET();

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            result.Ipv4.Family = AddressFamily.IPv4;
            var bytes = address.GetAddressBytes();
            result.Ipv4.Address = new IN_ADDR(bytes[0], bytes[1], bytes[2], bytes[3]);
        }
        else
        {
            result.Ipv6.Family = AddressFamily.IPv6;
            var bytes = address.GetAddressBytes();
            unsafe
            {
                for (int i = 0; i < 16; i++)
                {
                    result.Ipv6.Address.Bytes[i] = bytes[i];
                }
            }
        }

        return result;
    }

    private sealed record ManagedRoute(string Destination, ulong InterfaceLuid, MIB_IPFORWARD_ROW2 Row);
}
