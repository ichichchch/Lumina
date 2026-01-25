namespace Lumina.Native.WireGuardNT;

/// <summary>
/// WireGuard 接口配置结构。
/// 其后必须紧跟 <see cref="WIREGUARD_PEER"/> 结构数组。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct WIREGUARD_INTERFACE
{
    public WireGuardInterfaceFlags Flags;
    public ushort ListenPort;
    public fixed byte PrivateKey[32];
    public fixed byte PublicKey[32];
    public uint PeersCount;
}

/// <summary>
/// WireGuard Peer 配置结构。
/// 其后必须紧跟 <see cref="WIREGUARD_ALLOWED_IP"/> 结构数组。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct WIREGUARD_PEER
{
    public WireGuardPeerFlags Flags;
    public uint Reserved;
    public fixed byte PublicKey[32];
    public fixed byte PresharedKey[32];
    public ushort PersistentKeepalive;
    public SOCKADDR_INET Endpoint;
    public ulong TxBytes;
    public ulong RxBytes;
    public ulong LastHandshake;
    public uint AllowedIPsCount;
}

/// <summary>
/// WireGuard Allowed IP 配置结构。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct WIREGUARD_ALLOWED_IP
{
    public IN_ADDR Address;
    public IN6_ADDR AddressV6;
    public AddressFamily AddressFamily;
    public byte Cidr;
}

/// <summary>
/// IPv4 地址结构。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IN_ADDR
{
    public fixed byte Bytes[4];

    /// <summary>
    /// 使用 4 个字节初始化 <see cref="IN_ADDR"/>。
    /// </summary>
    /// <param name="b0">第 1 个字节。</param>
    /// <param name="b1">第 2 个字节。</param>
    /// <param name="b2">第 3 个字节。</param>
    /// <param name="b3">第 4 个字节。</param>
    public IN_ADDR(byte b0, byte b1, byte b2, byte b3)
    {
        Bytes[0] = b0;
        Bytes[1] = b1;
        Bytes[2] = b2;
        Bytes[3] = b3;
    }

    /// <summary>
    /// 从点分十进制字符串解析 IPv4 地址。
    /// </summary>
    /// <param name="ipAddress">IPv4 地址字符串（例如 "192.168.1.1"）。</param>
    /// <returns>解析得到的 <see cref="IN_ADDR"/>。</returns>
    /// <exception cref="FormatException">格式不合法时抛出。</exception>
    public static IN_ADDR Parse(string ipAddress)
    {
        var parts = ipAddress.Split('.');
        if (parts.Length != 4)
            throw new FormatException("Invalid IPv4 address format");

        var addr = new IN_ADDR();
        for (int i = 0; i < 4; i++)
        {
            addr.Bytes[i] = byte.Parse(parts[i]);
        }
        return addr;
    }

    /// <summary>
    /// 将地址格式化为点分十进制字符串。
    /// </summary>
    /// <returns>格式化后的 IPv4 地址字符串。</returns>
    public override readonly string ToString()
    {
        return $"{Bytes[0]}.{Bytes[1]}.{Bytes[2]}.{Bytes[3]}";
    }
}

/// <summary>
/// IPv6 地址结构。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IN6_ADDR
{
    public fixed byte Bytes[16];

    /// <summary>
    /// 将地址格式化为十六进制分段字符串。
    /// </summary>
    /// <returns>格式化后的 IPv6 地址字符串。</returns>
    public override readonly string ToString()
    {
        var parts = new string[8];
        for (int i = 0; i < 8; i++)
        {
            parts[i] = ((Bytes[i * 2] << 8) | Bytes[i * 2 + 1]).ToString("x4");
        }
        return string.Join(":", parts);
    }
}

/// <summary>
/// IPv4 Socket 地址结构。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SOCKADDR_IN
{
    public AddressFamily Family;
    public ushort Port;
    public IN_ADDR Address;
    private readonly ulong _padding;
}

/// <summary>
/// IPv6 Socket 地址结构。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SOCKADDR_IN6
{
    public AddressFamily Family;
    public ushort Port;
    public uint FlowInfo;
    public IN6_ADDR Address;
    public uint ScopeId;
}

/// <summary>
/// IPv4/IPv6 Socket 地址联合体结构。
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 28)]
public struct SOCKADDR_INET
{
    [FieldOffset(0)]
    public SOCKADDR_IN Ipv4;

    [FieldOffset(0)]
    public SOCKADDR_IN6 Ipv6;

    [FieldOffset(0)]
    public AddressFamily Family;
}
