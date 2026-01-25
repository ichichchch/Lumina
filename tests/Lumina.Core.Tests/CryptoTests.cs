namespace Lumina.Core.Tests;

// 针对 Curve25519 密钥生成器的单元测试：关注长度、clamping 规则与确定性等核心行为。
public class KeyGeneratorTests
{
    // 被测对象：Curve25519 私钥/公钥生成逻辑。
    private readonly IKeyGenerator _keyGenerator = new Curve25519KeyGenerator();

    [Fact]
    public void GeneratePrivateKey_Returns32Bytes()
    {
        var key = _keyGenerator.GeneratePrivateKey();

        Assert.Equal(32, key.Length);
    }

    [Fact]
    public void GeneratePrivateKey_IsClampedCorrectly()
    {
        var key = _keyGenerator.GeneratePrivateKey();

        // 校验 clamping：第 1 个字节的 bit0-bit2 必须清零
        Assert.Equal(0, key[0] & 0x07);

        // 最后 1 个字节：bit7 必须清零，bit6 必须置 1
        Assert.Equal(0, key[31] & 0x80);
        Assert.NotEqual(0, key[31] & 0x40);
    }

    [Fact]
    public void GeneratePrivateKey_ProducesUniqueKeys()
    {
        var key1 = _keyGenerator.GeneratePrivateKey();
        var key2 = _keyGenerator.GeneratePrivateKey();

        Assert.False(key1.SequenceEqual(key2));
    }

    [Fact]
    public void GetPublicKey_Returns32Bytes()
    {
        var privateKey = _keyGenerator.GeneratePrivateKey();
        var publicKey = _keyGenerator.GetPublicKey(privateKey);

        Assert.Equal(32, publicKey.Length);
    }

    [Fact]
    public void GetPublicKey_ThrowsForInvalidKeyLength()
    {
        var invalidKey = new byte[16];

        Assert.Throws<ArgumentException>(() => _keyGenerator.GetPublicKey(invalidKey));
    }

    [Fact]
    public void GetPublicKey_IsDeterministic()
    {
        var privateKey = _keyGenerator.GeneratePrivateKey();

        var publicKey1 = _keyGenerator.GetPublicKey(privateKey);
        var publicKey2 = _keyGenerator.GetPublicKey(privateKey);

        Assert.True(publicKey1.SequenceEqual(publicKey2));
    }

    [Fact]
    public void GenerateKeyPair_ReturnsValidBase64Strings()
    {
        var (privateKey, publicKey) = _keyGenerator.GenerateKeyPair();

        // Base64 编码后应为 44 个字符（32 字节 -> Base64）
        Assert.Equal(44, privateKey.Length);
        Assert.Equal(44, publicKey.Length);

        // 应能被正常解码，不抛出异常
        var privateBytes = Convert.FromBase64String(privateKey);
        var publicBytes = Convert.FromBase64String(publicKey);

        Assert.Equal(32, privateBytes.Length);
        Assert.Equal(32, publicBytes.Length);
    }

    [Fact]
    public void GenerateKeyPair_ProducesConsistentPairs()
    {
        var (privateKey, publicKey) = _keyGenerator.GenerateKeyPair();

        var privateBytes = Convert.FromBase64String(privateKey);
        var derivedPublic = _keyGenerator.GetPublicKey(privateBytes);
        var derivedPublicBase64 = Convert.ToBase64String(derivedPublic);

        Assert.Equal(publicKey, derivedPublicBase64);
    }
}
