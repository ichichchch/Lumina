namespace Lumina.Core.Crypto;

/// <summary>
/// WireGuard 密钥生成接口。
/// </summary>
public interface IKeyGenerator
{
    /// <summary>
    /// 生成新的 Curve25519 私钥。
    /// </summary>
    /// <returns>长度为 32 字节的私钥。</returns>
    byte[] GeneratePrivateKey();

    /// <summary>
    /// 根据私钥推导公钥。
    /// </summary>
    /// <param name="privateKey">长度为 32 字节的私钥。</param>
    /// <returns>长度为 32 字节的公钥。</returns>
    byte[] GetPublicKey(ReadOnlySpan<byte> privateKey);

    /// <summary>
    /// 生成新的密钥对。
    /// </summary>
    /// <returns>以 Base64 字符串表示的 (PrivateKey, PublicKey) 元组。</returns>
    (string PrivateKey, string PublicKey) GenerateKeyPair();
}
