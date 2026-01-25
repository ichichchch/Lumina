namespace Lumina.Core.Network;

/// <summary>
/// Windows 路由管理器：基于 IP Helper API 添加/删除路由。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRouteManager : IRouteManager, IDisposable
{
    private readonly ILogger<WindowsRouteManager>? _logger;
    private readonly List<ManagedRoute> _managedRoutes = [];
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 创建一个新的 Windows 路由管理器实例。
    /// </summary>
    /// <param name="logger">可选日志记录器。</param>
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
    /// 释放路由管理器并移除该实例管理的所有路由。
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

    /// <summary>
    /// 解析 CIDR 字符串，得到目标地址与前缀长度。
    /// </summary>
    /// <param name="cidr">CIDR 字符串。</param>
    /// <returns>地址与前缀长度。</returns>
    /// <exception cref="ArgumentException">CIDR 格式不合法时抛出。</exception>
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

    /// <summary>
    /// 根据 <see cref="IPAddress"/> 构造对应的 <see cref="SOCKADDR_INET"/> 结构。
    /// </summary>
    /// <param name="address">IPv4 或 IPv6 地址。</param>
    /// <returns>填充后的 <see cref="SOCKADDR_INET"/>。</returns>
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

    /// <summary>
    /// 表示由该实例添加并跟踪的路由记录。
    /// </summary>
    private sealed record ManagedRoute(string Destination, ulong InterfaceLuid, MIB_IPFORWARD_ROW2 Row);
}
