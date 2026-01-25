namespace Lumina.Core.Tests;

// TunnelConfiguration 的单元测试：验证配置字段校验与便捷属性行为。
public class TunnelConfigurationTests
{
    [Fact]
    public void Validate_ReturnsErrors_WhenNameIsEmpty()
    {
        var config = CreateValidConfiguration();
        config.Name = "";

        var errors = config.Validate();

        Assert.Contains(errors, e => e.Contains("Name"));
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenPrivateKeyIsMissing()
    {
        var config = CreateValidConfiguration();
        config.PrivateKey = null;
        config.PrivateKeyRef = null;

        var errors = config.Validate();

        Assert.Contains(errors, e => e.Contains("PrivateKey"));
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenPrivateKeyIsInvalid()
    {
        var config = CreateValidConfiguration();
        config.PrivateKey = "invalid-key";

        var errors = config.Validate();

        Assert.Contains(errors, e => e.Contains("PrivateKey"));
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenAddressesAreEmpty()
    {
        var config = CreateValidConfiguration();
        config.Addresses = [];

        var errors = config.Validate();

        Assert.Contains(errors, e => e.Contains("Address"));
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenPeersAreEmpty()
    {
        var config = CreateValidConfiguration();
        config.Peers = [];

        var errors = config.Validate();

        Assert.Contains(errors, e => e.Contains("Peer"));
    }

    [Fact]
    public void Validate_ReturnsNoErrors_ForValidConfiguration()
    {
        var config = CreateValidConfiguration();

        var errors = config.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void PrimaryEndpoint_ReturnsFirstPeerEndpoint()
    {
        var config = CreateValidConfiguration();

        Assert.Equal("1.2.3.4:51820", config.PrimaryEndpoint);
    }

    [Fact]
    public void PrimaryEndpoint_ReturnsNull_WhenNoPeers()
    {
        var config = CreateValidConfiguration();
        config.Peers = [];

        Assert.Null(config.PrimaryEndpoint);
    }

    private static TunnelConfiguration CreateValidConfiguration()
    {
        // 合法的 Base64 密钥（44 个字符，解码后为 32 字节）
        var validKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        return new TunnelConfiguration
        {
            Name = "Test Server",
            PrivateKey = validKey,
            Addresses = ["10.0.0.2/32"],
            Peers =
            [
                new PeerConfiguration
                {
                    PublicKey = validKey,
                    Endpoint = "1.2.3.4:51820",
                    AllowedIPs = ["0.0.0.0/0"]
                }
            ]
        };
    }
}

// PeerConfiguration 的单元测试：验证字段校验与端点解析逻辑。
public class PeerConfigurationTests
{
    [Fact]
    public void Validate_ReturnsErrors_WhenPublicKeyIsEmpty()
    {
        var peer = CreateValidPeer();
        peer.PublicKey = "";

        var errors = peer.Validate();

        Assert.Contains(errors, e => e.Contains("PublicKey"));
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenEndpointIsInvalid()
    {
        var peer = CreateValidPeer();
        peer.Endpoint = "invalid";

        var errors = peer.Validate();

        Assert.Contains(errors, e => e.Contains("Endpoint"));
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenAllowedIPsAreEmpty()
    {
        var peer = CreateValidPeer();
        peer.AllowedIPs = [];

        var errors = peer.Validate();

        Assert.Contains(errors, e => e.Contains("AllowedIP"));
    }

    [Fact]
    public void TryParseEndpoint_ParsesIPv4Endpoint()
    {
        var result = PeerConfiguration.TryParseEndpoint("192.168.1.1:51820", out var address, out var port);

        Assert.True(result);
        Assert.NotNull(address);
        Assert.Equal("192.168.1.1", address.ToString());
        Assert.Equal(51820, port);
    }

    [Fact]
    public void TryParseEndpoint_ParsesIPv6Endpoint()
    {
        var result = PeerConfiguration.TryParseEndpoint("[::1]:51820", out var address, out var port);

        Assert.True(result);
        Assert.NotNull(address);
        Assert.Equal(51820, port);
    }

    [Fact]
    public void TryParseEndpoint_ReturnsFalse_ForInvalidEndpoint()
    {
        var result = PeerConfiguration.TryParseEndpoint("invalid", out _, out _);

        Assert.False(result);
    }

    private static PeerConfiguration CreateValidPeer()
    {
        var validKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        return new PeerConfiguration
        {
            PublicKey = validKey,
            Endpoint = "1.2.3.4:51820",
            AllowedIPs = ["0.0.0.0/0"]
        };
    }
}

// TrafficStats 的单元测试：验证格式化输出与速度计算逻辑。
public class TrafficStatsTests
{
    [Fact]
    public void FormatBytes_FormatsCorrectly()
    {
        Assert.Equal("0.00 B", TrafficStats.FormatBytes(0));
        Assert.Equal("500.00 B", TrafficStats.FormatBytes(500));
        Assert.Equal("1.00 KB", TrafficStats.FormatBytes(1024));
        Assert.Equal("1.00 MB", TrafficStats.FormatBytes(1024 * 1024));
        Assert.Equal("1.00 GB", TrafficStats.FormatBytes(1024 * 1024 * 1024));
    }

    [Fact]
    public void FormatSpeed_FormatsCorrectly()
    {
        Assert.Equal("0.00 B/s", TrafficStats.FormatSpeed(0));
        Assert.Equal("1.00 KB/s", TrafficStats.FormatSpeed(1024));
    }

    [Fact]
    public void CalculateSpeed_ReturnsCurrentStats_WhenPreviousIsNull()
    {
        var current = new TrafficStats { TxBytes = 1000, RxBytes = 2000 };

        var result = TrafficStats.CalculateSpeed(current, null);

        Assert.Equal(current.TxBytes, result.TxBytes);
        Assert.Equal(current.RxBytes, result.RxBytes);
    }

    [Fact]
    public void CalculateSpeed_CalculatesCorrectSpeed()
    {
        var previous = new TrafficStats
        {
            TxBytes = 0,
            RxBytes = 0,
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(-1)
        };

        var current = new TrafficStats
        {
            TxBytes = 1024,
            RxBytes = 2048,
            Timestamp = DateTimeOffset.UtcNow
        };

        var result = TrafficStats.CalculateSpeed(current, previous);

        // TX 速度应约等于 1024 bytes/sec
        Assert.True(result.TxBytesPerSecond > 900 && result.TxBytesPerSecond < 1200);
        // RX 速度应约等于 2048 bytes/sec
        Assert.True(result.RxBytesPerSecond > 1800 && result.RxBytesPerSecond < 2400);
    }

    [Fact]
    public void HasHandshake_ReturnsFalse_WhenLastHandshakeIsZero()
    {
        var stats = new TrafficStats { LastHandshakeTime = 0 };

        Assert.False(stats.HasHandshake);
    }

    [Fact]
    public void HasHandshake_ReturnsTrue_WhenLastHandshakeIsSet()
    {
        var stats = new TrafficStats { LastHandshakeTime = 1234567890 };

        Assert.True(stats.HasHandshake);
    }
}

// ConnectionState 的单元测试：验证枚举包含预期的状态值。
public class ConnectionStateTests
{
    [Fact]
    public void ConnectionState_HasAllExpectedValues()
    {
        var values = Enum.GetValues<ConnectionState>();

        Assert.Contains(ConnectionState.Disconnected, values);
        Assert.Contains(ConnectionState.Connecting, values);
        Assert.Contains(ConnectionState.Connected, values);
        Assert.Contains(ConnectionState.Disconnecting, values);
        Assert.Contains(ConnectionState.Error, values);
    }
}
