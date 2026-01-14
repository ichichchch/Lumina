using System.Text.Json.Serialization;

namespace Lumina.Core.Models;

/// <summary>
/// Represents a complete WireGuard tunnel configuration.
/// </summary>
public sealed class TunnelConfiguration
{
    /// <summary>
    /// Unique identifier for this configuration.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for this tunnel.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Network adapter name (max 127 characters).
    /// </summary>
    public string InterfaceName { get; set; } = "Lumina0";

    /// <summary>
    /// Interface private key (Base64 encoded, 44 characters).
    /// Stored encrypted, not serialized directly.
    /// </summary>
    [JsonIgnore]
    public string? PrivateKey { get; set; }

    /// <summary>
    /// Reference to the encrypted private key storage.
    /// </summary>
    public string? PrivateKeyRef { get; set; }

    /// <summary>
    /// Interface addresses (CIDR notation).
    /// </summary>
    public required string[] Addresses { get; set; }

    /// <summary>
    /// Listen port (0 = random).
    /// </summary>
    public ushort ListenPort { get; set; }

    /// <summary>
    /// DNS servers for this interface.
    /// </summary>
    public string[] DnsServers { get; set; } = [];

    /// <summary>
    /// Peer configurations.
    /// </summary>
    public required List<PeerConfiguration> Peers { get; set; }

    /// <summary>
    /// Whether this is the default/favorite configuration.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Server location display text.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Last recorded latency in milliseconds.
    /// </summary>
    public int? LatencyMs { get; set; }

    /// <summary>
    /// When this configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this configuration was last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Validates the tunnel configuration.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
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

        return errors;
    }

    /// <summary>
    /// Gets the primary endpoint from the first peer.
    /// </summary>
    [JsonIgnore]
    public string? PrimaryEndpoint => Peers?.FirstOrDefault()?.Endpoint;

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
