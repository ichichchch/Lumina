namespace Lumina.Core.Network;

/// <summary>
/// Interface for IP route management.
/// </summary>
public interface IRouteManager
{
    /// <summary>
    /// Adds a route to the routing table.
    /// </summary>
    /// <param name="destination">Destination network in CIDR notation (e.g., "0.0.0.0/0").</param>
    /// <param name="interfaceLuid">Interface LUID to route through.</param>
    /// <param name="metric">Route metric (lower = higher priority).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddRouteAsync(string destination, ulong interfaceLuid, uint metric = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a route from the routing table.
    /// </summary>
    /// <param name="destination">Destination network in CIDR notation.</param>
    /// <param name="interfaceLuid">Interface LUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteRouteAsync(string destination, ulong interfaceLuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds routes for all allowed IPs.
    /// </summary>
    /// <param name="allowedIps">Array of allowed IP CIDRs.</param>
    /// <param name="interfaceLuid">Interface LUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddRoutesForAllowedIpsAsync(string[] allowedIps, ulong interfaceLuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all routes added by this manager.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAllManagedRoutesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of routes currently managed by this instance.
    /// </summary>
    IReadOnlyList<string> ManagedRoutes { get; }
}
