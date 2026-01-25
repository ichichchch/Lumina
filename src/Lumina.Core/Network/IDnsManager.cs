namespace Lumina.Core.Network;

/// <summary>
/// DNS 管理接口。
/// </summary>
public interface IDnsManager
{
    /// <summary>
    /// 为指定网络接口设置 DNS 服务器。
    /// </summary>
    /// <param name="interfaceGuid">网络接口的 GUID。</param>
    /// <param name="dnsServers">DNS 服务器地址数组。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SetDnsServersAsync(Guid interfaceGuid, string[] dnsServers, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将 DNS 设置恢复到调用 <see cref="SetDnsServersAsync"/> 之前的状态。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task RestoreDnsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 DNS 设置是否已被该管理器修改过。
    /// </summary>
    bool HasModifiedDns { get; }
}
