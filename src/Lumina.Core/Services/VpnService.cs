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
    private readonly ILogger<VpnService>? _logger;

    private readonly BehaviorSubject<ConnectionState> _connectionStateSubject;
    private readonly Subject<TrafficStats> _trafficStatsSubject;

    private WireGuardAdapterHandle? _adapterHandle;
    private TunnelConfiguration? _currentConfiguration;
    private IDisposable? _statsSubscription;
    private TrafficStats? _previousStats;
    private Guid? _adapterGuid;
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
        ILogger<VpnService>? logger = null)
    {
        _driver = driver;
        _driverManager = driverManager;
        _routeManager = routeManager;
        _dnsManager = dnsManager;
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

        // 校验配置
        var validationErrors = configuration.Validate();
        if (validationErrors.Count > 0)
        {
            throw new InvalidConfigurationException(configuration.Name, [.. validationErrors]);
        }

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

            // 步骤 6：为适配器添加 IP 地址
            await AddInterfaceAddressesAsync(luid, configuration.Addresses, cancellationToken);

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

    /// <summary>
    /// 停止统计信息轮询并释放订阅。
    /// </summary>
    private void StopStatsPolling()
    {
        _statsSubscription?.Dispose();
        _statsSubscription = null;
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
                    _logger?.LogDebug("Added address {Address} to interface", address);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error adding address {Address}", address);
            }
        }
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
