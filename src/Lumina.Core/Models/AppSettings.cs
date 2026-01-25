namespace Lumina.Core.Models;

/// <summary>
/// 应用级设置。
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// 应用启动时是否自动连接。
    /// </summary>
    public bool AutoConnect { get; set; }

    /// <summary>
    /// 自动连接所使用的配置 ID。
    /// </summary>
    public Guid? AutoConnectConfigId { get; set; }

    /// <summary>
    /// 是否随 Windows 启动自动启动应用。
    /// </summary>
    public bool StartOnBoot { get; set; }

    /// <summary>
    /// 是否以最小化方式启动（最小化到托盘）。
    /// </summary>
    public bool StartMinimized { get; set; }

    /// <summary>
    /// 是否启用 Kill Switch（当 VPN 异常断开时阻断所有流量）。
    /// </summary>
    public bool KillSwitch { get; set; }

    /// <summary>
    /// 主题偏好。
    /// </summary>
    public ThemeMode Theme { get; set; } = ThemeMode.Auto;

    /// <summary>
    /// 应用日志级别。
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// 统计信息刷新间隔（毫秒）。
    /// </summary>
    public int StatsUpdateIntervalMs { get; set; } = 1000;

    /// <summary>
    /// UI 语言（Culture Name，例如 "en-US"、"zh-CN"）。为空则跟随系统。
    /// </summary>
    public string? Language { get; set; }
}

/// <summary>
/// 应用主题模式。
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// 浅色主题。
    /// </summary>
    Light,

    /// <summary>
    /// 深色主题。
    /// </summary>
    Dark,

    /// <summary>
    /// 跟随系统主题。
    /// </summary>
    Auto,
}

/// <summary>
/// 日志级别。
/// </summary>
public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error,
}
