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
    public async Task AddRouteAsync(string destination, ulong interfaceLuid, uint metric = 0, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // 对于默认路由 0.0.0.0/0，拆分成两条更具体的路由以避免 Windows API 限制
        if (destination == "0.0.0.0/0")
        {
            _logger?.LogDebug("Splitting default route 0.0.0.0/0 into 0.0.0.0/1 and 128.0.0.0/1");
            await AddSingleRouteAsync("0.0.0.0/1", interfaceLuid, metric, cancellationToken);
            await AddSingleRouteAsync("128.0.0.0/1", interfaceLuid, metric, cancellationToken);
            return;
        }

        // 对于 IPv6 默认路由 ::/0，同样拆分
        if (destination == "::/0")
        {
            _logger?.LogDebug("Splitting default route ::/0 into ::/1 and 8000::/1");
            await AddSingleRouteAsync("::/1", interfaceLuid, metric, cancellationToken);
            await AddSingleRouteAsync("8000::/1", interfaceLuid, metric, cancellationToken);
            return;
        }

        await AddSingleRouteAsync(destination, interfaceLuid, metric, cancellationToken);
    }

    /// <summary>
    /// 添加单个路由项。
    /// </summary>
    private async Task AddSingleRouteAsync(string destination, ulong interfaceLuid, uint metric, CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Adding route {Destination} via interface LUID {InterfaceLuid}", destination, interfaceLuid);

        var (address, prefixLength) = ParseCidr(destination);

        // 对于默认路由等情况，添加重试机制以处理网络栈时序问题
        const int maxRetries = 5;
        const int retryDelayMs = 200;
        uint lastError = 0;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = TryAddRoute(destination, address, prefixLength, interfaceLuid, metric, out lastError);

            if (result)
            {
                lock (_lock)
                {
                    _managedRoutes.Add(new ManagedRoute(destination, interfaceLuid, default));
                }

                _logger?.LogInformation("Added route {Destination} via interface LUID {InterfaceLuid}", destination, interfaceLuid);
                return;
            }

            // 如果是 ERROR_INVALID_PARAMETER (87)，可能是网络栈还未就绪，重试
            if (lastError == IpHelperNative.ERROR_INVALID_PARAMETER && attempt < maxRetries - 1)
            {
                _logger?.LogDebug("Route add failed with error 87, retrying in {DelayMs}ms (attempt {Attempt}/{MaxRetries})",
                    retryDelayMs, attempt + 1, maxRetries);
                await Task.Delay(retryDelayMs, cancellationToken);
                continue;
            }

            break;
        }

        _logger?.LogError("Failed to add route {Destination}: error {ErrorCode}", destination, lastError);
        throw new RouteConfigurationException(destination, lastError);
    }

    /// <summary>
    /// 尝试添加路由项。
    /// </summary>
    /// <param name="destination">目标 CIDR。</param>
    /// <param name="address">目标地址。</param>
    /// <param name="prefixLength">前缀长度。</param>
    /// <param name="interfaceLuid">接口 LUID。</param>
    /// <param name="metric">路由度量。</param>
    /// <param name="errorCode">输出错误码。</param>
    /// <returns>成功返回 true，否则返回 false。</returns>
    private bool TryAddRoute(string destination, IPAddress address, byte prefixLength, ulong interfaceLuid, uint metric, out uint errorCode)
    {
        IpHelperNative.InitializeIpForwardEntry(out var row);

        row.InterfaceLuid = interfaceLuid;
        var indexResult = IpHelperNative.ConvertInterfaceLuidToIndex(in interfaceLuid, out var interfaceIndex);
        if (indexResult != IpHelperNative.NO_ERROR)
        {
            _logger?.LogError("Failed to resolve interface index from LUID {InterfaceLuid}: error {ErrorCode}", interfaceLuid, indexResult);
            errorCode = indexResult;
            return false;
        }
        row.InterfaceIndex = interfaceIndex;
        row.DestinationPrefix.PrefixLength = prefixLength;
        row.DestinationPrefix.Prefix = CreateSockaddrInet(address);

        // 获取 NextHop
        var nextHopResult = GetInterfaceNextHop(address, interfaceLuid);
        if (nextHopResult.HasValue)
        {
            row.NextHop = nextHopResult.Value;
        }
        else
        {
            // 回退到 on-link 路由
            row.NextHop = CreateSockaddrInet(
                address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    ? IPAddress.Any
                    : IPAddress.IPv6Any);
        }

        row.ValidLifetime = uint.MaxValue;
        row.PreferredLifetime = uint.MaxValue;
        row.Metric = metric;
        row.Protocol = NL_ROUTE_PROTOCOL.Nt_Static;
        row.Origin = NL_ROUTE_ORIGIN.Manual;
        row.Immortal = true;

        errorCode = IpHelperNative.CreateIpForwardEntry2(in row);

        return errorCode == IpHelperNative.NO_ERROR || errorCode == IpHelperNative.ERROR_OBJECT_ALREADY_EXISTS;
    }

    /// <summary>
    /// 尝试获取接口的下一跳地址。
    /// </summary>
    private SOCKADDR_INET? GetInterfaceNextHop(IPAddress destinationAddress, ulong interfaceLuid)
    {
        var family = destinationAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            ? (ushort)AddressFamily.IPv4
            : (ushort)AddressFamily.IPv6;

        var result = IpHelperNative.GetUnicastIpAddressTable(family, out var tablePtr);
        if (result != IpHelperNative.NO_ERROR || tablePtr == nint.Zero)
        {
            _logger?.LogDebug("GetUnicastIpAddressTable failed: error {ErrorCode}", result);
            return null;
        }

        try
        {
            var count = (uint)Marshal.ReadInt32(tablePtr);
            var rowSize = Marshal.SizeOf<MIB_UNICASTIPADDRESS_ROW>();
            var firstRowPtr = tablePtr + Marshal.SizeOf<MIB_UNICASTIPADDRESS_TABLE>();

            for (uint i = 0; i < count; i++)
            {
                var rowPtr = firstRowPtr + (int)(i * rowSize);
                var row = Marshal.PtrToStructure<MIB_UNICASTIPADDRESS_ROW>(rowPtr);

                if (row.InterfaceLuid == interfaceLuid)
                {
                    var nextHop = new SOCKADDR_INET();
                    if (destinationAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        nextHop.Ipv4.Family = AddressFamily.IPv4;
                        nextHop.Ipv4.Address = row.Address.Ipv4.Address;
                    }
                    else
                    {
                        nextHop.Ipv6.Family = AddressFamily.IPv6;
                        nextHop.Ipv6.Address = row.Address.Ipv6.Address;
                        nextHop.Ipv6.ScopeId = row.ScopeId;
                    }
                    return nextHop;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error in GetInterfaceNextHop");
            return null;
        }
        finally
        {
            IpHelperNative.FreeMibTable(tablePtr);
        }

        return null;
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
