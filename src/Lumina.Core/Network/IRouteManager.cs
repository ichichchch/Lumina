namespace Lumina.Core.Network;

/// <summary>
/// IP 路由管理接口。
/// </summary>
public interface IRouteManager
{
    /// <summary>
    /// 向路由表添加路由。
    /// </summary>
    /// <param name="destination">目标网络（CIDR 表示法，例如 "0.0.0.0/0"）。</param>
    /// <param name="interfaceLuid">要经过的接口 LUID。</param>
    /// <param name="metric">路由度量值（越小优先级越高）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AddRouteAsync(string destination, ulong interfaceLuid, uint metric = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从路由表删除路由。
    /// </summary>
    /// <param name="destination">目标网络（CIDR 表示法）。</param>
    /// <param name="interfaceLuid">接口 LUID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteRouteAsync(string destination, ulong interfaceLuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为所有允许的 IP 段添加路由。
    /// </summary>
    /// <param name="allowedIps">允许的 IP 段（CIDR）数组。</param>
    /// <param name="interfaceLuid">接口 LUID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AddRoutesForAllowedIpsAsync(string[] allowedIps, ulong interfaceLuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除该实例添加的所有路由。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task RemoveAllManagedRoutesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前由该实例管理的路由列表。
    /// </summary>
    IReadOnlyList<string> ManagedRoutes { get; }
}
