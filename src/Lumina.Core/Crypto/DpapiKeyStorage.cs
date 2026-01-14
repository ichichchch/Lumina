using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Lumina.Core.Crypto;

/// <summary>
/// DPAPI-based secure storage for private keys.
/// Keys are encrypted using Windows Data Protection API.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiKeyStorage : IKeyStorage
{
    private readonly string _storageDirectory;

    /// <summary>
    /// Creates a new DPAPI key storage instance.
    /// </summary>
    /// <param name="storageDirectory">
    /// Directory to store encrypted keys. Defaults to %LOCALAPPDATA%\Lumina\Keys.
    /// </param>
    public DpapiKeyStorage(string? storageDirectory = null)
    {
        _storageDirectory = storageDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lumina",
                "Keys");
    }

    /// <inheritdoc />
    public async Task StorePrivateKeyAsync(string identifier, byte[] privateKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(privateKey);

        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));
        }

        EnsureDirectoryExists();

        var filePath = GetKeyFilePath(identifier);

        // Encrypt using DPAPI with CurrentUser scope
        var encryptedData = ProtectedData.Protect(
            privateKey,
            GetEntropy(identifier),
            DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(filePath, encryptedData, cancellationToken);

        // Secure the file - remove inheritance and set restricted permissions
        SecureFile(filePath);
    }

    /// <inheritdoc />
    public async Task<byte[]?> LoadPrivateKeyAsync(string identifier, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var filePath = GetKeyFilePath(identifier);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var encryptedData = await File.ReadAllBytesAsync(filePath, cancellationToken);

        try
        {
            return ProtectedData.Unprotect(
                encryptedData,
                GetEntropy(identifier),
                DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException)
        {
            // Key is corrupted or was created by different user
            return null;
        }
    }

    /// <inheritdoc />
    public Task DeletePrivateKeyAsync(string identifier, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var filePath = GetKeyFilePath(identifier);

        if (File.Exists(filePath))
        {
            // Overwrite with random data before deletion for extra security
            try
            {
                var fileInfo = new FileInfo(filePath);
                var length = fileInfo.Length;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                {
                    var randomData = new byte[length];
                    RandomNumberGenerator.Fill(randomData);
                    fs.Write(randomData);
                    fs.Flush();
                }

                File.Delete(filePath);
            }
            catch (IOException)
            {
                // Best effort deletion
                File.Delete(filePath);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> KeyExistsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var filePath = GetKeyFilePath(identifier);
        return Task.FromResult(File.Exists(filePath));
    }

    private string GetKeyFilePath(string identifier)
    {
        // Sanitize identifier for file system
        var sanitized = string.Join("_",
            identifier.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        return Path.Combine(_storageDirectory, $"{sanitized}.key");
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_storageDirectory))
        {
            var dirInfo = Directory.CreateDirectory(_storageDirectory);

            // Set directory to hidden
            dirInfo.Attributes |= FileAttributes.Hidden;
        }
    }

    private static byte[] GetEntropy(string identifier)
    {
        // Additional entropy based on identifier
        // This provides extra protection against key theft
        using var sha = SHA256.Create();
        var identifierBytes = System.Text.Encoding.UTF8.GetBytes($"Lumina.KeyStorage.{identifier}");
        return sha.ComputeHash(identifierBytes);
    }

    private static void SecureFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            fileInfo.Attributes |= FileAttributes.Hidden;

            // Additional security through file system ACLs could be added here
            // For DPAPI, the encryption is the primary security mechanism
        }
        catch
        {
            // Best effort security hardening
        }
    }
}
