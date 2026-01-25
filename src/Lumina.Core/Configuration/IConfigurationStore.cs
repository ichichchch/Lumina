namespace Lumina.Core.Configuration;

/// <summary>
/// 隧道配置与应用设置的存储接口。
/// </summary>
public interface IConfigurationStore
{
    /// <summary>
    /// 保存隧道配置。
    /// </summary>
    /// <param name="configuration">要保存的配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveConfigurationAsync(TunnelConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 ID 加载隧道配置。
    /// </summary>
    /// <param name="id">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果找到则返回配置；否则返回 null。</returns>
    Task<TunnelConfiguration?> LoadConfigurationAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据名称加载隧道配置。
    /// </summary>
    /// <param name="name">配置名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果找到则返回配置；否则返回 null。</returns>
    Task<TunnelConfiguration?> LoadConfigurationByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载全部隧道配置。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>全部配置列表。</returns>
    Task<List<TunnelConfiguration>> LoadAllConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除隧道配置。
    /// </summary>
    /// <param name="id">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteConfigurationAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存应用设置。
    /// </summary>
    /// <param name="settings">要保存的设置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载应用设置。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>应用设置。</returns>
    Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);
}
