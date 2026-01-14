using System.Net;
using System.Text.Json.Serialization;

namespace Lumina.Core.Models;

/// <summary>
/// Represents a WireGuard peer configuration.
/// </summary>
public sealed class PeerConfiguration
{
    /// <summary>
    /// Peer's public key (Base64 encoded, 44 characters).
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    /// Optional pre-shared key for additional security (Base64 encoded).
    /// </summary>
    public string? PresharedKey { get; set; }

    /// <summary>
    /// Peer's endpoint address (IP:Port).
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Allowed IP ranges for this peer (CIDR notation).
    /// </summary>
    public required string[] AllowedIPs { get; set; }

    /// <summary>
    /// Persistent keepalive interval in seconds (0 = disabled).
    /// </summary>
    public ushort PersistentKeepalive { get; set; } = 25;

    /// <summary>
    /// Validates the peer configuration.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
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
    /// Parses an endpoint string into IP address and port.
    /// </summary>
    public static bool TryParseEndpoint(string endpoint, out IPAddress? address, out int port)
    {
        address = null;
        port = 0;

        if (string.IsNullOrWhiteSpace(endpoint))
            return false;

        // Handle IPv6 format: [address]:port
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

        // Handle IPv4 format: address:port
        var lastColon = endpoint.LastIndexOf(':');
        if (lastColon < 0)
            return false;

        var ip = endpoint[..lastColon];
        var portStr = endpoint[(lastColon + 1)..];

        if (!IPAddress.TryParse(ip, out address))
            return false;

        return int.TryParse(portStr, out port) && port > 0 && port <= 65535;
    }

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
