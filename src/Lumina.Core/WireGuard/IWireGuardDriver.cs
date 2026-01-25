namespace Lumina.Core.WireGuard;

/// <summary>
/// WireGuard 驱动操作接口。
/// </summary>
public interface IWireGuardDriver
{
    /// <summary>
    /// 创建新的 WireGuard 适配器。
    /// </summary>
    /// <param name="name">适配器名称（最大 127 个字符）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建得到的适配器句柄。</returns>
    Task<WireGuardAdapterHandle> CreateAdapterAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开已存在的 WireGuard 适配器。
    /// </summary>
    /// <param name="name">适配器名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>适配器句柄；未找到则返回 null。</returns>
    Task<WireGuardAdapterHandle?> OpenAdapterAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用指定隧道配置设置 WireGuard 适配器。
    /// </summary>
    /// <param name="handle">适配器句柄。</param>
    /// <param name="configuration">隧道配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SetConfigurationAsync(WireGuardAdapterHandle handle, TunnelConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从适配器获取当前流量统计信息。
    /// </summary>
    /// <param name="handle">适配器句柄。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前流量统计。</returns>
    Task<TrafficStats> GetStatsAsync(WireGuardAdapterHandle handle, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取适配器的 LUID。
    /// </summary>
    /// <param name="handle">适配器句柄。</param>
    /// <returns>适配器 LUID。</returns>
    ulong GetAdapterLuid(WireGuardAdapterHandle handle);

    /// <summary>
    /// 设置适配器状态（up/down）。
    /// </summary>
    /// <param name="handle">适配器句柄。</param>
    /// <param name="up">true 表示启用；false 表示禁用。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SetAdapterStateAsync(WireGuardAdapterHandle handle, bool up, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查 WireGuard 驱动是否已安装。
    /// </summary>
    /// <returns>已安装则返回 true。</returns>
    bool IsDriverInstalled();

    /// <summary>
    /// 获取驱动版本。
    /// </summary>
    /// <returns>驱动版本；未安装则返回 null。</returns>
    Version? GetDriverVersion();
}
