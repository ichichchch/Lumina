namespace Lumina.Core.Models;

/// <summary>
/// 表示完整的 WireGuard 隧道配置。
/// </summary>
public sealed class TunnelConfiguration
{
    /// <summary>
    /// 该配置的唯一标识。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 隧道的显示名称。
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 网络适配器名称（最大 127 个字符）。
    /// </summary>
    public string InterfaceName { get; set; } = "Lumina0";

    /// <summary>
    /// 接口私钥（Base64 编码，通常为 44 个字符）。
    /// 该字段仅在运行时使用，不会直接序列化落盘；持久化时应通过加密存储引用实现。
    /// </summary>
    [JsonIgnore]
    public string? PrivateKey { get; set; }

    /// <summary>
    /// 指向加密私钥存储的引用标识。
    /// </summary>
    public string? PrivateKeyRef { get; set; }

    /// <summary>
    /// 接口地址列表（CIDR 表示法）。
    /// </summary>
    public required string[] Addresses { get; set; }

    /// <summary>
    /// 监听端口（0 表示随机）。
    /// </summary>
    public ushort ListenPort { get; set; }

    /// <summary>
    /// 该接口使用的 DNS 服务器列表。
    /// </summary>
    public string[] DnsServers { get; set; } = [];

    public int? Mtu { get; set; }

    /// <summary>
    /// Peer 配置列表。
    /// </summary>
    public required List<PeerConfiguration> Peers { get; set; }

    /// <summary>
    /// 是否为默认/收藏配置。
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// 服务器位置显示文本。
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 上次记录的延迟（毫秒）。
    /// </summary>
    public int? LatencyMs { get; set; }

    /// <summary>
    /// 配置创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 配置最后修改时间。
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 校验隧道配置的有效性。
    /// </summary>
    /// <returns>校验错误列表；如果有效则为空。</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Name is required");
        }

        if (string.IsNullOrWhiteSpace(InterfaceName))
        {
            errors.Add("InterfaceName is required");
        }
        else if (InterfaceName.Length > 127)
        {
            errors.Add("InterfaceName must be 127 characters or less");
        }

        if (string.IsNullOrWhiteSpace(PrivateKey) && string.IsNullOrWhiteSpace(PrivateKeyRef))
        {
            errors.Add("PrivateKey is required");
        }
        else if (!string.IsNullOrWhiteSpace(PrivateKey) && !IsValidBase64Key(PrivateKey))
        {
            errors.Add("PrivateKey must be a valid Base64 WireGuard key (44 characters)");
        }

        if (Addresses is null || Addresses.Length == 0)
        {
            errors.Add("At least one Address is required");
        }
        else
        {
            foreach (var address in Addresses)
            {
                if (!IsValidCidr(address))
                {
                    errors.Add($"Invalid address CIDR notation: {address}");
                }
            }
        }

        if (Peers is null || Peers.Count == 0)
        {
            errors.Add("At least one Peer is required");
        }
        else
        {
            for (int i = 0; i < Peers.Count; i++)
            {
                var peerErrors = Peers[i].Validate();
                foreach (var error in peerErrors)
                {
                    errors.Add($"Peer[{i}]: {error}");
                }
            }
        }

        if (Mtu.HasValue)
        {
            if (Mtu.Value < 576 || Mtu.Value > 9000)
            {
                errors.Add("MTU must be between 576 and 9000");
            }
        }

        return errors;
    }

    /// <summary>
    /// 从第一个 Peer 获取主端点（若不存在则为 null）。
    /// </summary>
    [JsonIgnore]
    public string? PrimaryEndpoint => Peers?.FirstOrDefault()?.Endpoint;

    /// <summary>
    /// 检查字符串是否为有效的 WireGuard Base64 密钥格式。
    /// </summary>
    /// <param name="key">Base64 密钥字符串。</param>
    /// <returns>如果格式有效则返回 true。</returns>
    private static bool IsValidBase64Key(string key)
    {
        if (key.Length != 44)
            return false;

        try
        {
            var bytes = Convert.FromBase64String(key);
            return bytes.Length == 32;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 校验 CIDR 字符串的基本格式是否合法（IPv4/IPv6）。
    /// </summary>
    /// <param name="cidr">CIDR 字符串。</param>
    /// <returns>如果格式合法则返回 true。</returns>
    private static bool IsValidCidr(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            return false;

        var parts = cidr.Split('/');
        if (parts.Length != 2)
            return false;

        if (!System.Net.IPAddress.TryParse(parts[0], out var address))
            return false;

        if (!int.TryParse(parts[1], out var prefix))
            return false;

        var maxPrefix = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        return prefix >= 0 && prefix <= maxPrefix;
    }
}
