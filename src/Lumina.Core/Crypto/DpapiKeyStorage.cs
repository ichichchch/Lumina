namespace Lumina.Core.Crypto;

/// <summary>
/// 基于 DPAPI 的私钥安全存储实现。
/// 使用 Windows 数据保护 API 对密钥进行加密保护。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiKeyStorage : IKeyStorage
{
    private readonly string _storageDirectory;

    /// <summary>
    /// 创建一个新的 DPAPI 私钥存储实例。
    /// </summary>
    /// <param name="storageDirectory">
    /// 存放加密密钥文件的目录；默认为 %LOCALAPPDATA%\Lumina\Keys。
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
        EnsureWritablePath(filePath);

        // 使用 DPAPI（CurrentUser 作用域）加密
        var encryptedData = ProtectedData.Protect(
            privateKey,
            GetEntropy(identifier),
            DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(filePath, encryptedData, cancellationToken);

        // 加固文件（隐藏等最佳努力）
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
            // 密钥已损坏或由不同用户创建
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
            // 删除前用随机数据覆盖（最佳努力的额外保护）
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
                // 最佳努力删除
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

    /// <summary>
    /// 获取指定标识对应的密钥文件路径。
    /// </summary>
    /// <param name="identifier">密钥唯一标识。</param>
    /// <returns>密钥文件完整路径。</returns>
    private string GetKeyFilePath(string identifier)
    {
        // 将标识转换为文件系统可用的文件名片段
        var sanitized = string.Join("_",
            identifier.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        return Path.Combine(_storageDirectory, $"{sanitized}.key");
    }

    /// <summary>
    /// 确保存储目录存在；如果新建目录则设置为隐藏。
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_storageDirectory))
        {
            var dirInfo = Directory.CreateDirectory(_storageDirectory);

            // 将目录设置为隐藏
            dirInfo.Attributes |= FileAttributes.Hidden;
        }
    }

    private void EnsureWritablePath(string filePath)
    {
        try
        {
            if (Directory.Exists(_storageDirectory))
            {
                var dirInfo = new DirectoryInfo(_storageDirectory);
                var attributes = dirInfo.Attributes;
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    dirInfo.Attributes = attributes & ~FileAttributes.ReadOnly;
                }
            }
        }
        catch
        {
        }

        try
        {
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// 基于标识生成额外熵，用于增强 DPAPI 的保护强度。
    /// </summary>
    /// <param name="identifier">密钥唯一标识。</param>
    /// <returns>额外熵字节数组。</returns>
    private static byte[] GetEntropy(string identifier)
    {
        // 基于标识的额外熵（用于提供额外保护）
        using var sha = SHA256.Create();
        var identifierBytes = System.Text.Encoding.UTF8.GetBytes($"Lumina.KeyStorage.{identifier}");
        return sha.ComputeHash(identifierBytes);
    }

    /// <summary>
    /// 对密钥文件执行额外的文件系统层面加固（最佳努力）。
    /// </summary>
    /// <param name="filePath">密钥文件路径。</param>
    private static void SecureFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            fileInfo.Attributes |= FileAttributes.Hidden;

            // 可在此处增加 ACL 等额外安全策略；DPAPI 加密仍是主要保护机制
        }
        catch
        {
            // 最佳努力加固
        }
    }
}
