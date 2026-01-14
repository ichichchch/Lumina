namespace Lumina.Core.Crypto;

/// <summary>
/// Interface for WireGuard key generation.
/// </summary>
public interface IKeyGenerator
{
    /// <summary>
    /// Generates a new Curve25519 private key.
    /// </summary>
    /// <returns>32-byte private key.</returns>
    byte[] GeneratePrivateKey();

    /// <summary>
    /// Derives the public key from a private key.
    /// </summary>
    /// <param name="privateKey">32-byte private key.</param>
    /// <returns>32-byte public key.</returns>
    byte[] GetPublicKey(ReadOnlySpan<byte> privateKey);

    /// <summary>
    /// Generates a new key pair.
    /// </summary>
    /// <returns>Tuple of (PrivateKey, PublicKey) as Base64 strings.</returns>
    (string PrivateKey, string PublicKey) GenerateKeyPair();
}
