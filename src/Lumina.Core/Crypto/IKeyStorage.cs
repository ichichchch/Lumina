namespace Lumina.Core.Crypto;

/// <summary>
/// 私钥安全存储接口。
/// </summary>
public interface IKeyStorage
{
    /// <summary>
    /// 安全地存储私钥。
    /// </summary>
    /// <param name="identifier">密钥的唯一标识。</param>
    /// <param name="privateKey">要存储的私钥字节数组。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task StorePrivateKeyAsync(string identifier, byte[] privateKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从安全存储中加载私钥。
    /// </summary>
    /// <param name="identifier">密钥的唯一标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果找到则返回私钥字节数组；否则返回 null。</returns>
    Task<byte[]?> LoadPrivateKeyAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从存储中删除私钥。
    /// </summary>
    /// <param name="identifier">密钥的唯一标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeletePrivateKeyAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查存储中是否存在指定密钥。
    /// </summary>
    /// <param name="identifier">密钥的唯一标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果密钥存在则返回 true。</returns>
    Task<bool> KeyExistsAsync(string identifier, CancellationToken cancellationToken = default);
}
