namespace Lumina.Core.Models;

/// <summary>
/// Represents real-time traffic statistics for a VPN tunnel.
/// </summary>
public sealed class TrafficStats
{
    /// <summary>
    /// Total bytes transmitted.
    /// </summary>
    public ulong TxBytes { get; init; }

    /// <summary>
    /// Total bytes received.
    /// </summary>
    public ulong RxBytes { get; init; }

    /// <summary>
    /// Current upload speed in bytes per second.
    /// </summary>
    public double TxBytesPerSecond { get; init; }

    /// <summary>
    /// Current download speed in bytes per second.
    /// </summary>
    public double RxBytesPerSecond { get; init; }

    /// <summary>
    /// Time of the last successful handshake (Unix timestamp).
    /// </summary>
    public ulong LastHandshakeTime { get; init; }

    /// <summary>
    /// Timestamp when these stats were collected.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the tunnel has completed at least one handshake.
    /// </summary>
    public bool HasHandshake => LastHandshakeTime > 0;

    /// <summary>
    /// Calculates speed based on previous stats.
    /// </summary>
    /// <param name="previous">Previous stats sample.</param>
    /// <returns>New stats with calculated speeds.</returns>
    public static TrafficStats CalculateSpeed(TrafficStats current, TrafficStats? previous)
    {
        if (previous is null)
        {
            return current;
        }

        var timeDelta = (current.Timestamp - previous.Timestamp).TotalSeconds;
        if (timeDelta <= 0)
        {
            return current;
        }

        var txDelta = current.TxBytes >= previous.TxBytes
            ? current.TxBytes - previous.TxBytes
            : current.TxBytes;

        var rxDelta = current.RxBytes >= previous.RxBytes
            ? current.RxBytes - previous.RxBytes
            : current.RxBytes;

        return new TrafficStats
        {
            TxBytes = current.TxBytes,
            RxBytes = current.RxBytes,
            TxBytesPerSecond = txDelta / timeDelta,
            RxBytesPerSecond = rxDelta / timeDelta,
            LastHandshakeTime = current.LastHandshakeTime,
            Timestamp = current.Timestamp,
        };
    }

    /// <summary>
    /// Formats bytes as a human-readable string.
    /// </summary>
    public static string FormatBytes(ulong bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F2} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Formats speed as a human-readable string.
    /// </summary>
    public static string FormatSpeed(double bytesPerSecond)
    {
        return $"{FormatBytes((ulong)bytesPerSecond)}/s";
    }
}
