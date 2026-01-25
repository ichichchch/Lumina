using System.Runtime.InteropServices;
using Lumina.Native.WireGuardNT;
using Xunit;

namespace Lumina.Native.Tests;

// WireGuardNT 互操作层的单元测试：主要验证结构体大小、枚举值与基础解析行为。
public class WireGuardStructTests
{
    [Fact]
    public void WIREGUARD_INTERFACE_HasCorrectSize()
    {
        // WIREGUARD_INTERFACE 结构预期包含：
        // - Flags：2 字节（ushort）
        // - ListenPort：2 字节（ushort）
        // - PrivateKey：32 字节（fixed）
        // - PublicKey：32 字节（fixed）
        // - PeersCount：4 字节（uint）
        // 并采用 Pack=8 对齐
        var size = Marshal.SizeOf<WIREGUARD_INTERFACE>();

        // 该结构体应至少为 72 字节
        Assert.True(size >= 72, $"WIREGUARD_INTERFACE size is {size}, expected >= 72");
    }

    [Fact]
    public void WIREGUARD_PEER_HasCorrectSize()
    {
        // WIREGUARD_PEER 结构预期包含：
        // - Flags：4 字节（uint）
        // - Reserved：4 字节（uint）
        // - PublicKey：32 字节
        // - PresharedKey：32 字节
        // - PersistentKeepalive：2 字节（ushort）
        // - Endpoint：28 字节（SOCKADDR_INET）
        // - TxBytes：8 字节（ulong）
        // - RxBytes：8 字节（ulong）
        // - LastHandshake：8 字节（ulong）
        // - AllowedIPsCount：4 字节（uint）
        var size = Marshal.SizeOf<WIREGUARD_PEER>();

        // 该结构体尺寸应足够大（下限用于防止布局明显错误）
        Assert.True(size >= 130, $"WIREGUARD_PEER size is {size}, expected >= 130");
    }

    [Fact]
    public void SOCKADDR_INET_HasCorrectSize()
    {
        var size = Marshal.SizeOf<SOCKADDR_INET>();

        // SOCKADDR_INET 固定为 28 字节
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

// SafeHandle 相关单元测试：验证句柄无效/有效判定规则符合预期。
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
        // 使用一个非 0 且非 -1 的值，模拟有效指针句柄
        using var handle = new WireGuardAdapterHandle(new nint(1234), false);
        Assert.False(handle.IsInvalid);
    }
}

// WireGuard 相关枚举单元测试：验证枚举值与原生定义保持一致。
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
