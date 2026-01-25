namespace Lumina.Core.Models;

/// <summary>
/// 表示 VPN 隧道的实时流量统计信息。
/// </summary>
public sealed class TrafficStats
{
    /// <summary>
    /// 累计发送字节数。
    /// </summary>
    public ulong TxBytes { get; init; }

    /// <summary>
    /// 累计接收字节数。
    /// </summary>
    public ulong RxBytes { get; init; }

    /// <summary>
    /// 当前上传速率（字节/秒）。
    /// </summary>
    public double TxBytesPerSecond { get; init; }

    /// <summary>
    /// 当前下载速率（字节/秒）。
    /// </summary>
    public double RxBytesPerSecond { get; init; }

    /// <summary>
    /// 上次成功握手时间（Unix 时间戳）。
    /// </summary>
    public ulong LastHandshakeTime { get; init; }

    /// <summary>
    /// 采集该统计信息的时间戳。
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 隧道是否至少完成过一次握手。
    /// </summary>
    public bool HasHandshake => LastHandshakeTime > 0;

    /// <summary>
    /// 基于前一次采样计算速率。
    /// </summary>
    /// <param name="current">当前采样。</param>
    /// <param name="previous">前一次采样；为 null 时将不计算速率。</param>
    /// <returns>包含计算后速率字段的统计对象。</returns>
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
    /// 将字节数格式化为可读字符串。
    /// </summary>
    /// <param name="bytes">字节数。</param>
    /// <returns>格式化后的字符串。</returns>
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
    /// 将速率（字节/秒）格式化为可读字符串。
    /// </summary>
    /// <param name="bytesPerSecond">每秒字节数。</param>
    /// <returns>格式化后的字符串。</returns>
    public static string FormatSpeed(double bytesPerSecond)
    {
        return $"{FormatBytes((ulong)bytesPerSecond)}/s";
    }
}
