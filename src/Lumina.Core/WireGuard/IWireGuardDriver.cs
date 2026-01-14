using Lumina.Core.Models;
using Lumina.Native.WireGuardNT;

namespace Lumina.Core.WireGuard;

/// <summary>
/// Interface for WireGuard driver operations.
/// </summary>
public interface IWireGuardDriver
{
    /// <summary>
    /// Creates a new WireGuard adapter.
    /// </summary>
    /// <param name="name">Adapter name (max 127 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Handle to the created adapter.</returns>
    Task<WireGuardAdapterHandle> CreateAdapterAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an existing WireGuard adapter.
    /// </summary>
    /// <param name="name">Adapter name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Handle to the adapter, or null if not found.</returns>
    Task<WireGuardAdapterHandle?> OpenAdapterAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures a WireGuard adapter with the specified tunnel configuration.
    /// </summary>
    /// <param name="handle">Adapter handle.</param>
    /// <param name="configuration">Tunnel configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetConfigurationAsync(WireGuardAdapterHandle handle, TunnelConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current traffic statistics from the adapter.
    /// </summary>
    /// <param name="handle">Adapter handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current traffic statistics.</returns>
    Task<TrafficStats> GetStatsAsync(WireGuardAdapterHandle handle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the LUID of the adapter.
    /// </summary>
    /// <param name="handle">Adapter handle.</param>
    /// <returns>Adapter LUID.</returns>
    ulong GetAdapterLuid(WireGuardAdapterHandle handle);

    /// <summary>
    /// Sets the adapter state (up/down).
    /// </summary>
    /// <param name="handle">Adapter handle.</param>
    /// <param name="up">True to bring up, false to bring down.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAdapterStateAsync(WireGuardAdapterHandle handle, bool up, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the WireGuard driver is installed.
    /// </summary>
    /// <returns>True if installed.</returns>
    bool IsDriverInstalled();

    /// <summary>
    /// Gets the driver version.
    /// </summary>
    /// <returns>Driver version, or null if not installed.</returns>
    Version? GetDriverVersion();
}
