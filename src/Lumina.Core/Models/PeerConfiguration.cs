namespace Lumina.Core.Models;

/// <summary>
/// 表示 WireGuard Peer 的配置。
/// </summary>
public sealed class PeerConfiguration
{
    /// <summary>
    /// Peer 的公钥（Base64 编码，通常为 44 个字符）。
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    /// 可选的预共享密钥（Base64 编码），用于增强安全性。
    /// </summary>
    public string? PresharedKey { get; set; }

    /// <summary>
    /// Peer 的端点地址（IP:Port）。
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// 该 Peer 允许的 IP 段（CIDR 表示法）。
    /// </summary>
    public required string[] AllowedIPs { get; set; }

    /// <summary>
    /// 持久保活间隔（秒）；0 表示禁用。
    /// </summary>
    public ushort PersistentKeepalive { get; set; } = 25;

    /// <summary>
    /// 校验 Peer 配置的有效性。
    /// </summary>
    /// <returns>校验错误列表；如果有效则为空。</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(PublicKey))
        {
            errors.Add("PublicKey is required");
        }
        else if (!IsValidBase64Key(PublicKey))
        {
            errors.Add("PublicKey must be a valid Base64 WireGuard key (44 characters)");
        }

        if (!string.IsNullOrWhiteSpace(PresharedKey) && !IsValidBase64Key(PresharedKey))
        {
            errors.Add("PresharedKey must be a valid Base64 WireGuard key (44 characters)");
        }

        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            errors.Add("Endpoint is required");
        }
        else if (!TryParseEndpoint(Endpoint, out _, out _))
        {
            errors.Add("Endpoint must be in format 'IP:Port' or '[IPv6]:Port'");
        }

        if (AllowedIPs is null || AllowedIPs.Length == 0)
        {
            errors.Add("At least one AllowedIP is required");
        }
        else
        {
            foreach (var ip in AllowedIPs)
            {
                if (!IsValidCidr(ip))
                {
                    errors.Add($"Invalid CIDR notation: {ip}");
                }
            }
        }

        return errors;
    }

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
    /// 将端点字符串解析为 IP 地址与端口。
    /// </summary>
    /// <param name="endpoint">端点字符串。</param>
    /// <param name="address">解析得到的 IP 地址。</param>
    /// <param name="port">解析得到的端口。</param>
    /// <returns>如果解析成功则返回 true。</returns>
    public static bool TryParseEndpoint(string endpoint, out IPAddress? address, out int port)
    {
        address = null;
        port = 0;

        if (string.IsNullOrWhiteSpace(endpoint))
            return false;

        // IPv6 格式：[address]:port
        if (endpoint.StartsWith('['))
        {
            var closeBracket = endpoint.IndexOf(']');
            if (closeBracket < 0 || closeBracket + 2 >= endpoint.Length || endpoint[closeBracket + 1] != ':')
                return false;

            var ipPart = endpoint[1..closeBracket];
            var portPart = endpoint[(closeBracket + 2)..];

            if (!IPAddress.TryParse(ipPart, out address))
                return false;

            return int.TryParse(portPart, out port) && port > 0 && port <= 65535;
        }

        // IPv4 格式：address:port
        var lastColon = endpoint.LastIndexOf(':');
        if (lastColon < 0)
            return false;

        var ip = endpoint[..lastColon];
        var portStr = endpoint[(lastColon + 1)..];

        if (!IPAddress.TryParse(ip, out address))
            return false;

        return int.TryParse(portStr, out port) && port > 0 && port <= 65535;
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

        if (!IPAddress.TryParse(parts[0], out var address))
            return false;

        if (!int.TryParse(parts[1], out var prefix))
            return false;

        var maxPrefix = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        return prefix >= 0 && prefix <= maxPrefix;
    }
}
