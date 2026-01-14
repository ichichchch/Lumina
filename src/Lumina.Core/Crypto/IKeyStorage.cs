namespace Lumina.Core.Crypto;

/// <summary>
/// Interface for secure storage of private keys.
/// </summary>
public interface IKeyStorage
{
    /// <summary>
    /// Stores a private key securely.
    /// </summary>
    /// <param name="identifier">Unique identifier for the key.</param>
    /// <param name="privateKey">The private key bytes to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StorePrivateKeyAsync(string identifier, byte[] privateKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a private key from secure storage.
    /// </summary>
    /// <param name="identifier">Unique identifier for the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The private key bytes, or null if not found.</returns>
    Task<byte[]?> LoadPrivateKeyAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a private key from storage.
    /// </summary>
    /// <param name="identifier">Unique identifier for the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeletePrivateKeyAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in storage.
    /// </summary>
    /// <param name="identifier">Unique identifier for the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key exists.</returns>
    Task<bool> KeyExistsAsync(string identifier, CancellationToken cancellationToken = default);
}
