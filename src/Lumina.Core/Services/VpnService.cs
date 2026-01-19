namespace Lumina.Core.Services;

/// <summary>
/// Core VPN service implementation.
/// Orchestrates adapter creation, configuration, routing, and DNS.
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
    /// Creates a new VPN service instance.
    /// </summary>
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

        // Validate configuration
        var validationErrors = configuration.Validate();
        if (validationErrors.Count > 0)
        {
            throw new InvalidConfigurationException(configuration.Name, [.. validationErrors]);
        }

        SetState(ConnectionState.Connecting);

        try
        {
            // Step 1: Ensure driver is installed and running
            _logger?.LogDebug("Ensuring driver is ready");
            var driverResult = await _driverManager.EnsureDriverReadyAsync(cancellationToken);
            if (!driverResult.Success)
            {
                throw new DriverNotFoundException(
                    $"Failed to initialize WireGuard driver: {driverResult.ErrorMessage}");
            }
            _logger?.LogInformation("Driver state: {State}", driverResult.State);

            // Step 2: Create adapter
            _logger?.LogDebug("Creating adapter: {AdapterName}", configuration.InterfaceName);
            _adapterHandle = await _driver.CreateAdapterAsync(configuration.InterfaceName, cancellationToken);

            // Step 3: Configure tunnel
            _logger?.LogDebug("Setting tunnel configuration");
            await _driver.SetConfigurationAsync(_adapterHandle, configuration, cancellationToken);

            // Step 4: Get adapter LUID for routing
            var luid = _driver.GetAdapterLuid(_adapterHandle);
            _logger?.LogDebug("Adapter LUID: {Luid}", luid);

            // Step 5: Set adapter state to up
            await _driver.SetAdapterStateAsync(_adapterHandle, true, cancellationToken);

            // Step 6: Add IP address to adapter
            await AddInterfaceAddressesAsync(luid, configuration.Addresses, cancellationToken);

            // Step 7: Add routes for allowed IPs
            if (configuration.Peers.Count > 0)
            {
                var allAllowedIps = configuration.Peers
                    .SelectMany(p => p.AllowedIPs)
                    .Distinct()
                    .ToArray();

                _logger?.LogDebug("Adding routes for {Count} allowed IPs", allAllowedIps.Length);
                await _routeManager.AddRoutesForAllowedIpsAsync(allAllowedIps, luid, cancellationToken);
            }

            // Step 8: Configure DNS
            if (configuration.DnsServers.Length > 0)
            {
                _logger?.LogDebug("Setting DNS servers: {DnsServers}", string.Join(", ", configuration.DnsServers));

                // Get adapter GUID from interface
                _adapterGuid = await GetAdapterGuidAsync(configuration.InterfaceName, cancellationToken);

                if (_adapterGuid.HasValue)
                {
                    await _dnsManager.SetDnsServersAsync(_adapterGuid.Value, configuration.DnsServers, cancellationToken);
                }
            }

            // Step 9: Start stats polling
            StartStatsPolling();

            _currentConfiguration = configuration;
            SetState(ConnectionState.Connected);

            _logger?.LogInformation("Connected to {ConfigurationName}", configuration.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect: {Message}", ex.Message);

            // Rollback on failure
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
            // Stop stats polling
            StopStatsPolling();

            // Restore DNS
            if (_dnsManager.HasModifiedDns)
            {
                await _dnsManager.RestoreDnsAsync(cancellationToken);
            }

            // Remove routes
            await _routeManager.RemoveAllManagedRoutesAsync(cancellationToken);

            // Close adapter (triggers SafeHandle disposal)
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
    /// Disposes the VPN service.
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

    private void SetState(ConnectionState state)
    {
        _logger?.LogDebug("State changed: {OldState} -> {NewState}", _connectionStateSubject.Value, state);
        _connectionStateSubject.OnNext(state);
    }

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

    private void StopStatsPolling()
    {
        _statsSubscription?.Dispose();
        _statsSubscription = null;
    }

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

    private static async Task<Guid?> GetAdapterGuidAsync(string adapterName, CancellationToken cancellationToken)
    {
        // Try to find the adapter by name and get its GUID
        await Task.Yield(); // Make async

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
            // Ignore errors
        }

        return null;
    }
}
