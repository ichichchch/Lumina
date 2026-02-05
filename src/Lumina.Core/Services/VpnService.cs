namespace Lumina.Core.Services;

/// <summary>
/// 核心 VPN 服务实现。
/// 负责编排适配器创建、隧道配置、路由与 DNS 设置等流程。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class VpnService : IVpnService, IDisposable
{
    private readonly IWireGuardDriver _driver;
    private readonly IDriverManager _driverManager;
    private readonly IRouteManager _routeManager;
    private readonly IDnsManager _dnsManager;
    private readonly IKeyStorage _keyStorage;
    private readonly ILogger<VpnService>? _logger;

    private readonly BehaviorSubject<ConnectionState> _connectionStateSubject;
    private readonly Subject<TrafficStats> _trafficStatsSubject;

    private WireGuardAdapterHandle? _adapterHandle;
    private TunnelConfiguration? _currentConfiguration;
    private IDisposable? _statsSubscription;
    private TrafficStats? _previousStats;
    private Guid? _adapterGuid;
    private ulong _lastLoggedHandshakeTime;
    private int _noHandshakeLogCounter;
    private readonly List<MIB_IPFORWARD_ROW2> _endpointBypassRoutes = [];
    private bool _disposed;

    /// <summary>
    /// 创建一个新的 VPN 服务实例。
    /// </summary>
    /// <param name="driver">WireGuard 驱动抽象，用于创建适配器与配置隧道。</param>
    /// <param name="driverManager">驱动管理器，用于确保驱动已安装且就绪。</param>
    /// <param name="routeManager">路由管理器，用于添加/移除路由。</param>
    /// <param name="dnsManager">DNS 管理器，用于设置/恢复 DNS。</param>
    /// <param name="logger">可选日志记录器。</param>
    public VpnService(
        IWireGuardDriver driver,
        IDriverManager driverManager,
        IRouteManager routeManager,
        IDnsManager dnsManager,
        IKeyStorage keyStorage,
        ILogger<VpnService>? logger = null)
    {
        _driver = driver;
        _driverManager = driverManager;
        _routeManager = routeManager;
        _dnsManager = dnsManager;
        _keyStorage = keyStorage;
        _logger = logger;

        _connectionStateSubject = new BehaviorSubject<ConnectionState>(ConnectionState.Disconnected);
        _trafficStatsSubject = new Subject<TrafficStats>();
    }

    /// <inheritdoc />
    public ConnectionState CurrentState => _connectionStateSubject.Value;

    /// <inheritdoc />
    public TunnelConfiguration? CurrentConfiguration => _currentConfiguration;

    /// <inheritdoc />
    public IObservable<ConnectionState> ConnectionStateStream => _connectionStateSubject.AsObservable();

    /// <inheritdoc />
    public IObservable<TrafficStats> TrafficStatsStream => _trafficStatsSubject.AsObservable();

    /// <inheritdoc />
    public async Task ConnectAsync(TunnelConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(configuration);

        if (CurrentState is ConnectionState.Connected or ConnectionState.Connecting)
        {
            _logger?.LogWarning("Already connected or connecting, disconnecting first");
            await DisconnectAsync(cancellationToken);
        }

        _logger?.LogInformation("Connecting to {ConfigurationName}", configuration.Name);

        await EnsurePrivateKeyLoadedAsync(configuration, cancellationToken);
        NormalizeConfiguration(configuration);

        // 校验配置
        var validationErrors = configuration.Validate();
        if (validationErrors.Count > 0)
        {
            throw new InvalidConfigurationException(configuration.Name, [.. validationErrors]);
        }

        var peerEndpoints = configuration.Peers.Select(p => p.Endpoint).ToArray();
        var allowedIpCount = configuration.Peers.Sum(p => p.AllowedIPs.Length);
        _logger?.LogInformation(
            "Config: Interface={InterfaceName}, Addresses={Addresses}, Dns={DnsServers}, Mtu={Mtu}, Peers={PeerCount}, AllowedIPs={AllowedIpCount}, Endpoints={Endpoints}",
            configuration.InterfaceName,
            string.Join(", ", configuration.Addresses),
            string.Join(", ", configuration.DnsServers),
            configuration.Mtu,
            configuration.Peers.Count,
            allowedIpCount,
            string.Join(", ", peerEndpoints));

        SetState(ConnectionState.Connecting);

        try
        {
            // 步骤 1：确保驱动已安装且就绪
            _logger?.LogDebug("Ensuring driver is ready");
            var driverResult = await _driverManager.EnsureDriverReadyAsync(cancellationToken);
            if (!driverResult.Success)
            {
                throw new DriverNotFoundException(
                    $"Failed to initialize WireGuard driver: {driverResult.ErrorMessage}");
            }
            _logger?.LogInformation("Driver state: {State}", driverResult.State);

            // 步骤 2：创建适配器
            _logger?.LogDebug("Creating adapter: {AdapterName}", configuration.InterfaceName);
            _adapterHandle = await _driver.CreateAdapterAsync(configuration.InterfaceName, cancellationToken);

            // 步骤 3：配置隧道
            _logger?.LogDebug("Setting tunnel configuration");
            await _driver.SetConfigurationAsync(_adapterHandle, configuration, cancellationToken);

            // 步骤 4：获取适配器 LUID（用于路由）
            var luid = _driver.GetAdapterLuid(_adapterHandle);
            _logger?.LogDebug("Adapter LUID: {Luid}", luid);

            // 步骤 5：将适配器置为启用
            await _driver.SetAdapterStateAsync(_adapterHandle, true, cancellationToken);

            if (configuration.Mtu.HasValue && configuration.Mtu.Value > 0)
            {
                await ApplyMtuAsync(configuration.InterfaceName, configuration.Mtu.Value, cancellationToken);
            }

            // 步骤 6：为适配器添加 IP 地址
            await AddInterfaceAddressesAsync(luid, configuration.Addresses, cancellationToken);

            // 等待网络栈就绪（Windows 需要时间处理新添加的 IP 地址）
            await WaitForInterfaceReadyAsync(luid, cancellationToken);

            await AddEndpointBypassRoutesAsync(configuration, luid, cancellationToken);

            // 步骤 7：为 AllowedIPs 添加路由
            if (configuration.Peers.Count > 0)
            {
                var allAllowedIps = configuration.Peers
                    .SelectMany(p => p.AllowedIPs)
                    .Distinct()
                    .ToArray();

                _logger?.LogDebug("Adding routes for {Count} allowed IPs", allAllowedIps.Length);
                await _routeManager.AddRoutesForAllowedIpsAsync(allAllowedIps, luid, cancellationToken);
            }

            // 步骤 8：配置 DNS
            if (configuration.DnsServers.Length > 0)
            {
                _logger?.LogDebug("Setting DNS servers: {DnsServers}", string.Join(", ", configuration.DnsServers));

                // 获取适配器 GUID
                _adapterGuid = await GetAdapterGuidAsync(configuration.InterfaceName, cancellationToken);

                if (_adapterGuid.HasValue)
                {
                    await _dnsManager.SetDnsServersAsync(_adapterGuid.Value, configuration.DnsServers, cancellationToken);
                }
            }

            // 步骤 9：启动统计轮询
            StartStatsPolling();

            _currentConfiguration = configuration;
            SetState(ConnectionState.Connected);

            _logger?.LogInformation("Connected to {ConfigurationName}", configuration.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect: {Message}", ex.Message);

            // 失败回滚
            await RollbackAsync(cancellationToken);

            SetState(ConnectionState.Error);

            if (ex is LuminaException)
                throw;

            throw new ConnectionException(ConnectionFailureReason.Unknown, "Failed to establish connection", ex);
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (CurrentState == ConnectionState.Disconnected)
        {
            _logger?.LogDebug("Already disconnected");
            return;
        }

        _logger?.LogInformation("Disconnecting");

        SetState(ConnectionState.Disconnecting);

        try
        {
            // 停止统计轮询
            StopStatsPolling();

            // 恢复 DNS
            if (_dnsManager.HasModifiedDns)
            {
                await _dnsManager.RestoreDnsAsync(cancellationToken);
            }

            await RemoveEndpointBypassRoutesAsync();

            // 移除路由
            await _routeManager.RemoveAllManagedRoutesAsync(cancellationToken);

            // 关闭适配器（触发 SafeHandle 释放）
            if (_adapterHandle is not null && !_adapterHandle.IsInvalid)
            {
                _adapterHandle.Dispose();
                _adapterHandle = null;
            }

            _currentConfiguration = null;
            _adapterGuid = null;
            _previousStats = null;

            SetState(ConnectionState.Disconnected);

            _logger?.LogInformation("Disconnected");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during disconnect: {Message}", ex.Message);
            SetState(ConnectionState.Error);
        }
    }

    /// <inheritdoc />
    public bool IsDriverInstalled() => _driver.IsDriverInstalled();

    /// <inheritdoc />
    public Version? GetDriverVersion() => _driver.GetDriverVersion();

    /// <summary>
    /// 释放 VPN 服务并清理资源（最佳努力断开连接并释放订阅）。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        StopStatsPolling();

        try
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error during disposal");
        }

        _connectionStateSubject.Dispose();
        _trafficStatsSubject.Dispose();
    }

    /// <summary>
    /// 更新连接状态并向状态流推送最新值。
    /// </summary>
    /// <param name="state">新的连接状态。</param>
    private void SetState(ConnectionState state)
    {
        _logger?.LogDebug("State changed: {OldState} -> {NewState}", _connectionStateSubject.Value, state);
        _connectionStateSubject.OnNext(state);
    }

    /// <summary>
    /// 在连接流程失败时执行回滚：停止统计、恢复 DNS、移除路由并释放适配器。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步回滚的任务。</returns>
    private async Task RollbackAsync(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Rolling back connection");

        try
        {
            StopStatsPolling();

            if (_dnsManager.HasModifiedDns)
            {
                await _dnsManager.RestoreDnsAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error restoring DNS during rollback");
        }

        try
        {
            await RemoveEndpointBypassRoutesAsync();
            await _routeManager.RemoveAllManagedRoutesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error removing routes during rollback");
        }

        if (_adapterHandle is not null && !_adapterHandle.IsInvalid)
        {
            _adapterHandle.Dispose();
            _adapterHandle = null;
        }

        _currentConfiguration = null;
    }

    /// <summary>
    /// 启动定时轮询统计信息，并将结果推送到 <see cref="TrafficStatsStream"/>。
    /// </summary>
    private void StartStatsPolling()
    {
        StopStatsPolling();

        _logger?.LogDebug("Starting stats polling");

        _statsSubscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .Where(_ => CurrentState == ConnectionState.Connected && _adapterHandle is not null)
            .SelectMany(async _ =>
            {
                try
                {
                    var stats = await _driver.GetStatsAsync(_adapterHandle!, default);
                    var calculatedStats = TrafficStats.CalculateSpeed(stats, _previousStats);
                    if (calculatedStats.HasHandshake)
                    {
                        if (calculatedStats.LastHandshakeTime != _lastLoggedHandshakeTime)
                        {
                            _lastLoggedHandshakeTime = calculatedStats.LastHandshakeTime;
                            _noHandshakeLogCounter = 0;
                            var handshakeTime = DateTimeOffset.FromUnixTimeSeconds((long)calculatedStats.LastHandshakeTime).ToLocalTime();
                            _logger?.LogInformation(
                                "Handshake: {HandshakeTime} Tx={TxBytes} Rx={RxBytes} Up={TxSpeed} Down={RxSpeed} Peers={PeerCount} Details={PeerDetails}",
                                handshakeTime,
                                calculatedStats.TxBytes,
                                calculatedStats.RxBytes,
                                TrafficStats.FormatSpeed(calculatedStats.TxBytesPerSecond),
                                TrafficStats.FormatSpeed(calculatedStats.RxBytesPerSecond),
                                calculatedStats.PeerCount,
                                calculatedStats.PeerSummaries ?? "None");
                        }
                    }
                    else
                    {
                        _noHandshakeLogCounter++;
                        if (_noHandshakeLogCounter % 10 == 0)
                        {
                            _logger?.LogWarning(
                                "No handshake yet: LastHandshake={LastHandshake} Tx={TxBytes} Rx={RxBytes} Up={TxSpeed} Down={RxSpeed} Peers={PeerCount} Details={PeerDetails}",
                                calculatedStats.LastHandshakeTime,
                                calculatedStats.TxBytes,
                                calculatedStats.RxBytes,
                                TrafficStats.FormatSpeed(calculatedStats.TxBytesPerSecond),
                                TrafficStats.FormatSpeed(calculatedStats.RxBytesPerSecond),
                                calculatedStats.PeerCount,
                                calculatedStats.PeerSummaries ?? "None");
                        }
                    }
                    _previousStats = stats;
                    return calculatedStats;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error getting stats");
                    return new TrafficStats();
                }
            })
            .Subscribe(_trafficStatsSubject);
    }

    private async Task AddEndpointBypassRoutesAsync(TunnelConfiguration configuration, ulong tunnelLuid, CancellationToken cancellationToken)
    {
        if (configuration.Peers.Count == 0)
        {
            return;
        }

        var addedEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var peer in configuration.Peers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PeerConfiguration.TryParseEndpoint(peer.Endpoint, out var address, out _))
            {
                _logger?.LogWarning("Skipping endpoint route for invalid endpoint: {Endpoint}", peer.Endpoint);
                continue;
            }

            if (address is null)
            {
                continue;
            }

            var endpointKey = address.ToString();
            if (!addedEndpoints.Add(endpointKey))
            {
                continue;
            }

            var bestRoute = GetBestRouteForAddress(address, tunnelLuid);
            if (!bestRoute.HasValue)
            {
                _logger?.LogWarning("No route found for endpoint {Endpoint}", peer.Endpoint);
                continue;
            }

            var selectedRoute = bestRoute.Value;
            var interfaceName = TryGetInterfaceName(selectedRoute.InterfaceLuid);
            _logger?.LogInformation(
                "Endpoint route selection: Endpoint={Endpoint} Luid={Luid} Name={Name} Index={Index} Prefix={Prefix}/{PrefixLength} NextHop={NextHop} Metric={Metric}",
                peer.Endpoint,
                selectedRoute.InterfaceLuid,
                interfaceName ?? "Unknown",
                selectedRoute.InterfaceIndex,
                FormatSockaddrInet(selectedRoute.DestinationPrefix.Prefix),
                selectedRoute.DestinationPrefix.PrefixLength,
                FormatSockaddrInet(selectedRoute.NextHop),
                selectedRoute.Metric);

            var routeRow = CreateHostRoute(address, selectedRoute);
            var result = IpHelperNative.CreateIpForwardEntry2(in routeRow);

            if (result == IpHelperNative.NO_ERROR)
            {
                _endpointBypassRoutes.Add(routeRow);
                _logger?.LogInformation("Added endpoint bypass route for {Endpoint}", peer.Endpoint);
            }
            else if (result == IpHelperNative.ERROR_OBJECT_ALREADY_EXISTS)
            {
                _logger?.LogDebug("Endpoint bypass route already exists for {Endpoint}", peer.Endpoint);
            }
            else
            {
                _logger?.LogWarning("Failed to add endpoint bypass route for {Endpoint}: error {ErrorCode}", peer.Endpoint, result);
            }
        }
    }

    private Task RemoveEndpointBypassRoutesAsync()
    {
        if (_endpointBypassRoutes.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (var route in _endpointBypassRoutes)
        {
            var result = IpHelperNative.DeleteIpForwardEntry2(in route);
            if (result != IpHelperNative.NO_ERROR && result != IpHelperNative.ERROR_NOT_FOUND)
            {
                _logger?.LogWarning("Failed to remove endpoint bypass route: error {ErrorCode}", result);
            }
        }

        _endpointBypassRoutes.Clear();
        return Task.CompletedTask;
    }

    private static MIB_IPFORWARD_ROW2? GetBestRouteForAddress(IPAddress address, ulong excludedInterfaceLuid)
    {
        var family = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            ? (ushort)System.Net.Sockets.AddressFamily.InterNetwork
            : (ushort)System.Net.Sockets.AddressFamily.InterNetworkV6;

        var result = IpHelperNative.GetIpForwardTable2(family, out var tablePtr);
        if (result != IpHelperNative.NO_ERROR || tablePtr == nint.Zero)
        {
            return null;
        }

        try
        {
            var table = Marshal.PtrToStructure<MIB_IPFORWARD_TABLE2>(tablePtr);
            var count = table.NumEntries;
            var rowSize = Marshal.SizeOf<MIB_IPFORWARD_ROW2>();
            var firstRowPtr = tablePtr + Marshal.SizeOf<MIB_IPFORWARD_TABLE2>();

            MIB_IPFORWARD_ROW2? best = null;

            for (uint i = 0; i < count; i++)
            {
                var rowPtr = firstRowPtr + (int)(i * rowSize);
                var row = Marshal.PtrToStructure<MIB_IPFORWARD_ROW2>(rowPtr);

                if (row.InterfaceLuid == excludedInterfaceLuid || row.Loopback)
                {
                    continue;
                }

                if (!AddressMatchesPrefix(address, row.DestinationPrefix))
                {
                    continue;
                }

                if (!best.HasValue)
                {
                    best = row;
                    continue;
                }

                var bestRow = best.Value;
                if (row.DestinationPrefix.PrefixLength > bestRow.DestinationPrefix.PrefixLength ||
                    (row.DestinationPrefix.PrefixLength == bestRow.DestinationPrefix.PrefixLength && row.Metric < bestRow.Metric))
                {
                    best = row;
                }
            }

            return best;
        }
        finally
        {
            IpHelperNative.FreeMibTable(tablePtr);
        }
    }

    private static MIB_IPFORWARD_ROW2 CreateHostRoute(IPAddress address, MIB_IPFORWARD_ROW2 baseRoute)
    {
        var route = baseRoute;
        route.DestinationPrefix.Prefix = CreateSockaddrInet(address);
        route.DestinationPrefix.PrefixLength = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? (byte)32 : (byte)128;
        route.SitePrefixLength = 0;
        route.ValidLifetime = uint.MaxValue;
        route.PreferredLifetime = uint.MaxValue;
        route.Protocol = NL_ROUTE_PROTOCOL.Nt_Static;
        route.Origin = NL_ROUTE_ORIGIN.Manual;
        route.Immortal = true;
        return route;
    }

    private static bool AddressMatchesPrefix(IPAddress address, IP_ADDRESS_PREFIX prefix)
    {
        var addressBytes = address.GetAddressBytes();
        var prefixBytes = GetAddressBytes(prefix.Prefix, address.AddressFamily);
        var prefixLength = prefix.PrefixLength;

        if (prefixLength == 0)
        {
            return true;
        }

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (addressBytes[i] != prefixBytes[i])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (addressBytes[fullBytes] & mask) == (prefixBytes[fullBytes] & mask);
    }

    private static unsafe byte[] GetAddressBytes(SOCKADDR_INET address, System.Net.Sockets.AddressFamily family)
    {
        if (family == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return
            [
                address.Ipv4.Address.Bytes[0],
                address.Ipv4.Address.Bytes[1],
                address.Ipv4.Address.Bytes[2],
                address.Ipv4.Address.Bytes[3],
            ];
        }

        var bytes = new byte[16];
        for (var i = 0; i < 16; i++)
        {
            bytes[i] = address.Ipv6.Address.Bytes[i];
        }

        return bytes;
    }

    private static string FormatSockaddrInet(SOCKADDR_INET address)
    {
        return address.Family switch
        {
            AddressFamily.IPv4 => address.Ipv4.Address.ToString(),
            AddressFamily.IPv6 => address.Ipv6.Address.ToString(),
            _ => "Unknown"
        };
    }

    private static string? TryGetInterfaceName(ulong interfaceLuid)
    {
        var buffer = new char[256];
        var result = IpHelperNative.ConvertInterfaceLuidToName(in interfaceLuid, buffer, buffer.Length);
        if (result != IpHelperNative.NO_ERROR)
        {
            return null;
        }

        var name = new string(buffer);
        var nullIndex = name.IndexOf('\0');
        if (nullIndex >= 0)
        {
            name = name[..nullIndex];
        }

        return string.IsNullOrWhiteSpace(name) ? null : name;
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

    /// <summary>
    /// 停止统计信息轮询并释放订阅。
    /// </summary>
    private void StopStatsPolling()
    {
        _statsSubscription?.Dispose();
        _statsSubscription = null;
        _lastLoggedHandshakeTime = 0;
        _noHandshakeLogCounter = 0;
    }

    private async Task EnsurePrivateKeyLoadedAsync(TunnelConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(configuration.PrivateKey))
        {
            _logger?.LogInformation("Private key source: inline");
            try
            {
                var inlineBytes = Convert.FromBase64String(configuration.PrivateKey);
                if (inlineBytes.Length == 32)
                {
                    _logger?.LogInformation("Private key fingerprint: {Fingerprint}", ComputeKeyFingerprint(inlineBytes));
                }
                else
                {
                    _logger?.LogWarning("Private key invalid length after decode: {Length}", inlineBytes.Length);
                }
            }
            catch (FormatException)
            {
                _logger?.LogWarning("Private key is not valid Base64");
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.PrivateKeyRef))
        {
            _logger?.LogWarning("Private key source: missing (no inline key and no key reference)");
            return;
        }

        _logger?.LogInformation("Private key source: storage ({KeyRef})", configuration.PrivateKeyRef);
        var keyBytes = await _keyStorage.LoadPrivateKeyAsync(configuration.PrivateKeyRef, cancellationToken);
        if (keyBytes is not null)
        {
            configuration.PrivateKey = Convert.ToBase64String(keyBytes);
            _logger?.LogInformation("Private key fingerprint: {Fingerprint}", ComputeKeyFingerprint(keyBytes));
            return;
        }

        _logger?.LogWarning("Private key not found in storage: {KeyRef}", configuration.PrivateKeyRef);
        throw new InvalidConfigurationException($"PrivateKey not found for configuration '{configuration.Name}'.");
    }

    private static string ComputeKeyFingerprint(ReadOnlySpan<byte> keyBytes)
    {
        var hash = SHA256.HashData(keyBytes);
        var hex = Convert.ToHexString(hash);
        return hex.Length > 16 ? hex[..16] : hex;
    }

    private static void NormalizeConfiguration(TunnelConfiguration configuration)
    {
        configuration.Name = configuration.Name.Trim();
        configuration.InterfaceName = configuration.InterfaceName.Trim();
        configuration.PrivateKey = configuration.PrivateKey?.Trim();
        configuration.PrivateKeyRef = configuration.PrivateKeyRef?.Trim();
        configuration.Addresses = NormalizeList(configuration.Addresses);
        configuration.DnsServers = NormalizeList(configuration.DnsServers);

        foreach (var peer in configuration.Peers)
        {
            peer.PublicKey = peer.PublicKey.Trim();
            peer.PresharedKey = string.IsNullOrWhiteSpace(peer.PresharedKey) ? null : peer.PresharedKey.Trim();
            peer.Endpoint = peer.Endpoint.Trim();
            peer.AllowedIPs = NormalizeList(peer.AllowedIPs);
        }
    }

    private static string[] NormalizeList(string[]? items)
    {
        if (items is null)
        {
            return [];
        }

        return items
            .Select(item => item?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray()!;
    }

    private async Task ApplyMtuAsync(string interfaceName, int mtu, CancellationToken cancellationToken)
    {
        if (mtu < 576)
        {
            _logger?.LogWarning("MTU {Mtu} is too small, skipping apply", mtu);
            return;
        }

        await RunNetshCommandAsync(
            $"interface ipv4 set subinterface name=\"{interfaceName}\" mtu={mtu} store=active",
            cancellationToken);

        if (mtu >= 1280)
        {
            await RunNetshCommandAsync(
                $"interface ipv6 set subinterface name=\"{interfaceName}\" mtu={mtu} store=active",
                cancellationToken);
        }
    }

    private async Task RunNetshCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(error) ? output : error;
            _logger?.LogWarning("Netsh failed: {Arguments} ExitCode={ExitCode} Error={Error}", arguments, process.ExitCode, detail);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger?.LogDebug("Netsh output: {Arguments} Output={Output}", arguments, output.Trim());
            }
        }
    }

    /// <summary>
    /// 为指定接口添加一组单播地址。
    /// </summary>
    /// <param name="interfaceLuid">接口 LUID。</param>
    /// <param name="addresses">地址列表（CIDR 表示法）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步添加地址的任务。</returns>
    private async Task AddInterfaceAddressesAsync(ulong interfaceLuid, string[] addresses, CancellationToken cancellationToken)
    {
        foreach (var address in addresses)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var parts = address.Split('/');
                var ipAddress = IPAddress.Parse(parts[0]);
                var prefix = byte.Parse(parts[1]);

                IpHelperNative.InitializeUnicastIpAddressEntry(out var row);

                row.InterfaceLuid = interfaceLuid;
                row.OnLinkPrefixLength = prefix;
                row.DadState = NL_DAD_STATE.Preferred;

                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    row.Address.Ipv4.Family = AddressFamily.IPv4;
                    var bytes = ipAddress.GetAddressBytes();
                    row.Address.Ipv4.Address = new IN_ADDR(bytes[0], bytes[1], bytes[2], bytes[3]);
                }
                else
                {
                    row.Address.Ipv6.Family = AddressFamily.IPv6;
                    var bytes = ipAddress.GetAddressBytes();
                    unsafe
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            row.Address.Ipv6.Address.Bytes[i] = bytes[i];
                        }
                    }
                }

                var result = IpHelperNative.CreateUnicastIpAddressEntry(in row);

                if (result != IpHelperNative.NO_ERROR && result != IpHelperNative.ERROR_OBJECT_ALREADY_EXISTS)
                {
                    _logger?.LogWarning("Failed to add address {Address}: error {ErrorCode}", address, result);
                }
                else
                {
                _logger?.LogDebug("Added address {Address} to interface LUID {InterfaceLuid}", address, interfaceLuid);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error adding address {Address}", address);
            }
        }
    }

    /// <summary>
    /// 等待接口就绪（网络栈处理完新添加的 IP 地址）。
    /// </summary>
    /// <param name="interfaceLuid">接口 LUID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步等待的任务。</returns>
    private async Task WaitForInterfaceReadyAsync(ulong interfaceLuid, CancellationToken cancellationToken)
    {
        const int maxRetries = 10;
        const int delayMs = 100;

        for (int i = 0; i < maxRetries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 检查接口是否有 IP 地址已配置
            var result = IpHelperNative.GetUnicastIpAddressTable((ushort)AddressFamily.IPv4, out var tablePtr);
            if (result == IpHelperNative.NO_ERROR && tablePtr != nint.Zero)
            {
                try
                {
                    var count = (uint)Marshal.ReadInt32(tablePtr);
                    var rowSize = Marshal.SizeOf<MIB_UNICASTIPADDRESS_ROW>();
                    var firstRowPtr = tablePtr + Marshal.SizeOf<MIB_UNICASTIPADDRESS_TABLE>();

                    for (uint j = 0; j < count; j++)
                    {
                        var rowPtr = firstRowPtr + (int)(j * rowSize);
                        var row = Marshal.PtrToStructure<MIB_UNICASTIPADDRESS_ROW>(rowPtr);
                        if (row.InterfaceLuid == interfaceLuid && row.DadState == NL_DAD_STATE.Preferred)
                        {
                            _logger?.LogDebug("Interface ready after {Attempt} checks", i + 1);
                            return;
                        }
                    }
                }
                finally
                {
                    IpHelperNative.FreeMibTable(tablePtr);
                }
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        // 即使未检测到就绪状态，也继续尝试（可能是 IPv6 地址）
        _logger?.LogDebug("Interface ready check timed out, proceeding anyway");
    }

    /// <summary>
    /// 根据适配器名称尝试解析适配器 GUID。
    /// </summary>
    /// <param name="adapterName">适配器名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>适配器 GUID；如果未找到则返回 null。</returns>
    private static async Task<Guid?> GetAdapterGuidAsync(string adapterName, CancellationToken cancellationToken)
    {
        // 通过名称查找适配器并返回其 GUID
        await Task.Yield();

        try
        {
            var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var adapter = adapters.FirstOrDefault(a =>
                a.Name.Equals(adapterName, StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains(adapterName, StringComparison.OrdinalIgnoreCase));

            if (adapter is not null && Guid.TryParse(adapter.Id, out var guid))
            {
                return guid;
            }
        }
        catch
        {
            // 忽略错误
        }

        return null;
    }
}
