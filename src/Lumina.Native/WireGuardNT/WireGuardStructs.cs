using System.Runtime.InteropServices;

namespace Lumina.Native.WireGuardNT;

/// <summary>
/// WireGuard interface configuration structure.
/// Must be followed by WIREGUARD_PEER structures.
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
/// WireGuard peer configuration structure.
/// Must be followed by WIREGUARD_ALLOWED_IP structures.
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
/// WireGuard allowed IP configuration structure.
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
/// IPv4 address structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IN_ADDR
{
    public fixed byte Bytes[4];

    public IN_ADDR(byte b0, byte b1, byte b2, byte b3)
    {
        Bytes[0] = b0;
        Bytes[1] = b1;
        Bytes[2] = b2;
        Bytes[3] = b3;
    }

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

    public override readonly string ToString()
    {
        return $"{Bytes[0]}.{Bytes[1]}.{Bytes[2]}.{Bytes[3]}";
    }
}

/// <summary>
/// IPv6 address structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct IN6_ADDR
{
    public fixed byte Bytes[16];

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
/// Socket address for IPv4.
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
/// Socket address for IPv6.
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
/// Union of IPv4 and IPv6 socket addresses.
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
