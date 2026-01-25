namespace Lumina.Core.Services;

/// <summary>
/// 核心 VPN 服务接口。
/// </summary>
public interface IVpnService
{
    /// <summary>
    /// 获取当前连接状态。
    /// </summary>
    ConnectionState CurrentState { get; }

    /// <summary>
    /// 获取当前隧道配置（若已连接）。
    /// </summary>
    TunnelConfiguration? CurrentConfiguration { get; }

    /// <summary>
    /// 连接状态变化的可观察流。
    /// </summary>
    IObservable<ConnectionState> ConnectionStateStream { get; }

    /// <summary>
    /// 流量统计的可观察流（连接期间持续发出）。
    /// </summary>
    IObservable<TrafficStats> TrafficStatsStream { get; }

    /// <summary>
    /// 使用指定配置连接到 VPN。
    /// </summary>
    /// <param name="configuration">隧道配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task ConnectAsync(TunnelConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开 VPN 连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查 WireGuard 驱动是否已安装。
    /// </summary>
    /// <returns>已安装则返回 true。</returns>
    bool IsDriverInstalled();

    /// <summary>
    /// 获取 WireGuard 驱动版本。
    /// </summary>
    /// <returns>驱动版本；未安装则返回 null。</returns>
    Version? GetDriverVersion();
}
