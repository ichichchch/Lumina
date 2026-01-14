using System.Runtime.InteropServices;
using Lumina.Native.WireGuardNT;
using Xunit;

namespace Lumina.Native.Tests;

public class WireGuardStructTests
{
    [Fact]
    public void WIREGUARD_INTERFACE_HasCorrectSize()
    {
        // WIREGUARD_INTERFACE should have:
        // - Flags: 2 bytes (ushort)
        // - ListenPort: 2 bytes (ushort)
        // - PrivateKey: 32 bytes (fixed)
        // - PublicKey: 32 bytes (fixed)
        // - PeersCount: 4 bytes (uint)
        // With Pack=8 alignment
        var size = Marshal.SizeOf<WIREGUARD_INTERFACE>();

        // The struct should be 72 bytes minimum
        Assert.True(size >= 72, $"WIREGUARD_INTERFACE size is {size}, expected >= 72");
    }

    [Fact]
    public void WIREGUARD_PEER_HasCorrectSize()
    {
        // WIREGUARD_PEER should have:
        // - Flags: 4 bytes (uint)
        // - Reserved: 4 bytes (uint)
        // - PublicKey: 32 bytes
        // - PresharedKey: 32 bytes
        // - PersistentKeepalive: 2 bytes (ushort)
        // - Endpoint: 28 bytes (SOCKADDR_INET)
        // - TxBytes: 8 bytes (ulong)
        // - RxBytes: 8 bytes (ulong)
        // - LastHandshake: 8 bytes (ulong)
        // - AllowedIPsCount: 4 bytes (uint)
        var size = Marshal.SizeOf<WIREGUARD_PEER>();

        // The struct should be substantial
        Assert.True(size >= 130, $"WIREGUARD_PEER size is {size}, expected >= 130");
    }

    [Fact]
    public void SOCKADDR_INET_HasCorrectSize()
    {
        var size = Marshal.SizeOf<SOCKADDR_INET>();

        // SOCKADDR_INET is 28 bytes
        Assert.Equal(28, size);
    }

    [Fact]
    public void IN_ADDR_CanParse()
    {
        var addr = IN_ADDR.Parse("192.168.1.1");

        unsafe
        {
            Assert.Equal(192, addr.Bytes[0]);
            Assert.Equal(168, addr.Bytes[1]);
            Assert.Equal(1, addr.Bytes[2]);
            Assert.Equal(1, addr.Bytes[3]);
        }
    }

    [Fact]
    public void IN_ADDR_CanFormat()
    {
        var addr = new IN_ADDR(10, 0, 0, 1);
        var result = addr.ToString();

        Assert.Equal("10.0.0.1", result);
    }

    [Fact]
    public void IN_ADDR_Parse_ThrowsOnInvalidFormat()
    {
        Assert.Throws<FormatException>(() => IN_ADDR.Parse("invalid"));
        Assert.Throws<FormatException>(() => IN_ADDR.Parse("1.2.3"));
        Assert.Throws<FormatException>(() => IN_ADDR.Parse("1.2.3.4.5"));
    }
}

public class SafeHandleTests
{
    [Fact]
    public void WireGuardAdapterHandle_IsInvalidByDefault()
    {
        using var handle = new WireGuardAdapterHandle();
        Assert.True(handle.IsInvalid);
    }

    [Fact]
    public void WireGuardAdapterHandle_ZeroIsInvalid()
    {
        using var handle = new WireGuardAdapterHandle(nint.Zero, false);
        Assert.True(handle.IsInvalid);
    }

    [Fact]
    public void WireGuardAdapterHandle_MinusOneIsInvalid()
    {
        using var handle = new WireGuardAdapterHandle(new nint(-1), false);
        Assert.True(handle.IsInvalid);
    }

    [Fact]
    public void WireGuardAdapterHandle_ValidPointerIsValid()
    {
        // Use a non-zero, non-negative-one value
        using var handle = new WireGuardAdapterHandle(new nint(1234), false);
        Assert.False(handle.IsInvalid);
    }
}

public class WireGuardEnumTests
{
    [Fact]
    public void WireGuardInterfaceFlags_HasCorrectValues()
    {
        Assert.Equal(0, (int)WireGuardInterfaceFlags.None);
        Assert.Equal(1, (int)WireGuardInterfaceFlags.HasPublicKey);
        Assert.Equal(2, (int)WireGuardInterfaceFlags.HasPrivateKey);
        Assert.Equal(4, (int)WireGuardInterfaceFlags.HasListenPort);
        Assert.Equal(8, (int)WireGuardInterfaceFlags.ReplacePeers);
    }

    [Fact]
    public void WireGuardPeerFlags_HasCorrectValues()
    {
        Assert.Equal(0u, (uint)WireGuardPeerFlags.None);
        Assert.Equal(1u, (uint)WireGuardPeerFlags.HasPublicKey);
        Assert.Equal(2u, (uint)WireGuardPeerFlags.HasPresharedKey);
        Assert.Equal(4u, (uint)WireGuardPeerFlags.HasPersistentKeepalive);
        Assert.Equal(8u, (uint)WireGuardPeerFlags.HasEndpoint);
        Assert.Equal(32u, (uint)WireGuardPeerFlags.ReplaceAllowedIps);
        Assert.Equal(64u, (uint)WireGuardPeerFlags.Remove);
        Assert.Equal(128u, (uint)WireGuardPeerFlags.UpdateOnly);
    }

    [Fact]
    public void AddressFamily_HasCorrectValues()
    {
        Assert.Equal((ushort)0, (ushort)AddressFamily.Unspecified);
        Assert.Equal((ushort)2, (ushort)AddressFamily.IPv4);
        Assert.Equal((ushort)23, (ushort)AddressFamily.IPv6);
    }

    [Fact]
    public void WireGuardAdapterState_HasCorrectValues()
    {
        Assert.Equal(0u, (uint)WireGuardAdapterState.Down);
        Assert.Equal(1u, (uint)WireGuardAdapterState.Up);
    }
}
