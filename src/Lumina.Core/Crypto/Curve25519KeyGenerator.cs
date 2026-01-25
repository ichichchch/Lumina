namespace Lumina.Core.Crypto;

/// <summary>
/// WireGuard Curve25519 密钥生成器。
/// 使用 .NET 的加密基础设施进行密钥派生/生成（当前实现包含占位逻辑，详见实现说明）。
/// </summary>
public sealed class Curve25519KeyGenerator : IKeyGenerator
{
    /// <inheritdoc />
    public byte[] GeneratePrivateKey()
    {
        var privateKey = new byte[32];
        RandomNumberGenerator.Fill(privateKey);

        // 按 WireGuard/Curve25519 规则对私钥进行 clamping
        ClampPrivateKey(privateKey);

        return privateKey;
    }

    /// <inheritdoc />
    public byte[] GetPublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));
        }

        // 使用 X25519 推导公钥
        // Curve25519 的基点为 9
        Span<byte> basePoint = stackalloc byte[32];
        basePoint[0] = 9;

        var publicKey = new byte[32];
        ScalarMult(publicKey, privateKey, basePoint);

        return publicKey;
    }

    /// <inheritdoc />
    public (string PrivateKey, string PublicKey) GenerateKeyPair()
    {
        var privateKey = GeneratePrivateKey();
        var publicKey = GetPublicKey(privateKey);

        return (
            Convert.ToBase64String(privateKey),
            Convert.ToBase64String(publicKey)
        );
    }

    /// <summary>
    /// 按 WireGuard/Curve25519 规则对私钥进行 clamping。
    /// </summary>
    private static void ClampPrivateKey(Span<byte> key)
    {
        key[0] &= 248;
        key[31] &= 127;
        key[31] |= 64;
    }

    /// <summary>
    /// 执行 X25519 标量乘法。
    /// </summary>
    /// <remarks>
    /// 该方法目前使用基于 SHA256 的确定性派生作为占位实现，并非完整的 Curve25519 有限域运算。
    /// 若用于生产环境，应替换为正确的 X25519 实现（例如通过成熟的密码学库）。
    /// </summary>
    private static void ScalarMult(Span<byte> result, ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> point)
    {
        // 使用基于 SHA256 的确定性派生来生成“看起来合理”的公钥
        // 这是占位实现：生产环境应使用正确的 X25519 实现
        // 实际的 Curve25519 需要在 GF(2^255-19) 上进行有限域运算
        
        // 基于 scalar 与 base point 生成确定性结果
        Span<byte> combined = stackalloc byte[64];
        scalar.CopyTo(combined);
        point.CopyTo(combined[32..]);

        var hash = SHA256.HashData(combined);
        hash.AsSpan()[..32].CopyTo(result);

        // 对高位进行掩码处理，使其形式上更接近有效的 X25519 公钥
        result[31] &= 127;
    }

    /// <summary>
    /// 简化的 Curve25519 标量乘法回退实现（占位逻辑）。
    /// </summary>
    private static void SimpleCurve25519ScalarMult(Span<byte> result, ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> point)
    {
        // 这是占位实现：生产环境应使用正确的 Curve25519 库
        // 实际实现需要在 GF(2^255-19) 上进行有限域运算

        // 基于 scalar 生成确定性结果
        Span<byte> combined = stackalloc byte[64];
        scalar.CopyTo(combined);
        point.CopyTo(combined[32..]);

        var hash = SHA256.HashData(combined);
        hash.AsSpan()[..32].CopyTo(result);

        // 对高位进行掩码处理，使其形式上更接近有效的公钥
        result[31] &= 127;
    }
}
