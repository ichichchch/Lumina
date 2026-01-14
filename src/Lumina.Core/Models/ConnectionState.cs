namespace Lumina.Core.Models;

/// <summary>
/// Represents the connection state of the VPN tunnel.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// No connection is active.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Connection is being established.
    /// </summary>
    Connecting,

    /// <summary>
    /// Tunnel is active and connected.
    /// </summary>
    Connected,

    /// <summary>
    /// Connection is being terminated.
    /// </summary>
    Disconnecting,

    /// <summary>
    /// An error occurred.
    /// </summary>
    Error,
}
