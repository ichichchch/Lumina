namespace Lumina.Core.Services;

/// <summary>
/// Core VPN service interface.
/// </summary>
public interface IVpnService
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    ConnectionState CurrentState { get; }

    /// <summary>
    /// Gets the current tunnel configuration (if connected).
    /// </summary>
    TunnelConfiguration? CurrentConfiguration { get; }

    /// <summary>
    /// Observable stream of connection state changes.
    /// </summary>
    IObservable<ConnectionState> ConnectionStateStream { get; }

    /// <summary>
    /// Observable stream of traffic statistics (emits while connected).
    /// </summary>
    IObservable<TrafficStats> TrafficStatsStream { get; }

    /// <summary>
    /// Connects to the VPN using the specified configuration.
    /// </summary>
    /// <param name="configuration">Tunnel configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(TunnelConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the VPN.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the WireGuard driver is installed.
    /// </summary>
    /// <returns>True if installed.</returns>
    bool IsDriverInstalled();

    /// <summary>
    /// Gets the WireGuard driver version.
    /// </summary>
    /// <returns>Driver version, or null if not installed.</returns>
    Version? GetDriverVersion();
}
