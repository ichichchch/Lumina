using System;
using System.IO;
using Lumina.Native.WireGuardNT;
using Lumina.Native.WireGuardWindows;
using Xunit;

namespace Lumina.Native.Tests;

// embeddable-dll-service 的单元测试：不依赖真实 tunnel.dll/wireguard.dll，只验证参数分发与返回值约定。
public class EmbeddableTunnelServiceTests
{
    [Fact]
    public void TryRunFromServiceArgs_ReturnsFalse_WhenNotServiceMode()
    {
        var handled = EmbeddableTunnelService.TryRunFromServiceArgs(["--help"], out var exitCode);

        Assert.False(handled);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void TryRunFromServiceArgs_ReturnsHandled_WhenTunnelDllMissing()
    {
        var handled = EmbeddableTunnelService.TryRunFromServiceArgs(
            ["/service", "test.conf"],
            out var exitCode,
            tunnelDllPath: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "tunnel.dll"),
            wireGuardDllPath: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "wireguard.dll"));

        Assert.True(handled);
        Assert.Equal(1, exitCode);
    }
}

// WireGuardNT 动态加载辅助的单元测试：仅验证缺省环境下的安全行为（不尝试加载真实 dll）。
public class WireGuardNtLibraryLoaderTests
{
    [Fact]
    public void TryRegisterResolver_ReturnsFalse_WhenFileDoesNotExist()
    {
        var ok = WireGuardNtLibraryLoader.TryRegisterResolver(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "wireguard.dll"));

        Assert.False(ok);
    }
}

// Windows 服务安装辅助的单元测试：只验证字符串格式化逻辑，避免对 SCM 产生副作用。
public class WireGuardTunnelWindowsServiceInstallerTests
{
    [Fact]
    public void GetServiceName_FormatsCorrectly()
    {
        Assert.Equal("WireGuardTunnel$OfficeNet", WireGuardTunnelWindowsServiceInstaller.GetServiceName("OfficeNet"));
    }
}

