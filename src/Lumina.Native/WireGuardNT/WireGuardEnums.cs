namespace Lumina.Native.WireGuardNT;

/// <summary>
/// Flags for WireGuard interface configuration.
/// </summary>
[Flags]
public enum WireGuardInterfaceFlags : ushort
{
    None = 0,
    HasPublicKey = 1 << 0,
    HasPrivateKey = 1 << 1,
    HasListenPort = 1 << 2,
    ReplacePeers = 1 << 3,
}

/// <summary>
/// Flags for WireGuard peer configuration.
/// </summary>
[Flags]
public enum WireGuardPeerFlags : uint
{
    None = 0,
    HasPublicKey = 1 << 0,
    HasPresharedKey = 1 << 1,
    HasPersistentKeepalive = 1 << 2,
    HasEndpoint = 1 << 3,
    ReplaceAllowedIps = 1 << 5,
    Remove = 1 << 6,
    UpdateOnly = 1 << 7,
}

/// <summary>
/// Flags for WireGuard allowed IP configuration.
/// </summary>
[Flags]
public enum WireGuardAllowedIpFlags : uint
{
    None = 0,
}

/// <summary>
/// Address family for network addresses.
/// </summary>
public enum AddressFamily : ushort
{
    Unspecified = 0,
    IPv4 = 2,
    IPv6 = 23,
}

/// <summary>
/// WireGuard adapter state.
/// </summary>
public enum WireGuardAdapterState : uint
{
    Down = 0,
    Up = 1,
}

/// <summary>
/// Log level for WireGuard adapter logging.
/// </summary>
public enum WireGuardAdapterLogLevel : uint
{
    Off = 0,
    Error = 1,
    Warning = 2,
    Info = 3,
}
