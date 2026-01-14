namespace Lumina.Core.Network;

/// <summary>
/// Interface for DNS management.
/// </summary>
public interface IDnsManager
{
    /// <summary>
    /// Sets DNS servers for a network interface.
    /// </summary>
    /// <param name="interfaceGuid">GUID of the interface.</param>
    /// <param name="dnsServers">Array of DNS server addresses.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetDnsServersAsync(Guid interfaceGuid, string[] dnsServers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores DNS settings to the state before SetDnsServersAsync was called.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreDnsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether DNS settings have been modified by this manager.
    /// </summary>
    bool HasModifiedDns { get; }
}
