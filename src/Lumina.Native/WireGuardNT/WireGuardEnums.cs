namespace Lumina.Native.WireGuardNT;

/// <summary>
/// WireGuard 接口配置标志位。
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
/// WireGuard Peer 配置标志位。
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
/// WireGuard Allowed IP 配置标志位。
/// </summary>
[Flags]
public enum WireGuardAllowedIpFlags : uint
{
    None = 0,
}

/// <summary>
/// 网络地址的地址族。
/// </summary>
public enum AddressFamily : ushort
{
    Unspecified = 0,
    IPv4 = 2,
    IPv6 = 23,
}

/// <summary>
/// WireGuard 适配器状态。
/// </summary>
public enum WireGuardAdapterState : uint
{
    Down = 0,
    Up = 1,
}

/// <summary>
/// WireGuard 适配器日志级别。
/// </summary>
public enum WireGuardAdapterLogLevel : uint
{
    Off = 0,
    Error = 1,
    Warning = 2,
    Info = 3,
}
