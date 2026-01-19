namespace Lumina.Core.Tests;

public class KeyGeneratorTests
{
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

        // Check clamping: first byte has bits 0-2 cleared
        Assert.Equal(0, key[0] & 0x07);

        // Last byte has bit 7 cleared and bit 6 set
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

        // Should be valid Base64 (44 characters for 32 bytes)
        Assert.Equal(44, privateKey.Length);
        Assert.Equal(44, publicKey.Length);

        // Should decode without exception
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
