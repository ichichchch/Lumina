namespace Lumina.Core.Models;

/// <summary>
/// Application-level settings.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Automatically connect on application startup.
    /// </summary>
    public bool AutoConnect { get; set; }

    /// <summary>
    /// ID of the configuration to auto-connect.
    /// </summary>
    public Guid? AutoConnectConfigId { get; set; }

    /// <summary>
    /// Start the application on Windows startup.
    /// </summary>
    public bool StartOnBoot { get; set; }

    /// <summary>
    /// Start minimized to system tray.
    /// </summary>
    public bool StartMinimized { get; set; }

    /// <summary>
    /// Enable kill switch (block all traffic when VPN disconnects unexpectedly).
    /// </summary>
    public bool KillSwitch { get; set; }

    /// <summary>
    /// Theme preference.
    /// </summary>
    public ThemeMode Theme { get; set; } = ThemeMode.Auto;

    /// <summary>
    /// Log level for the application.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Statistics update interval in milliseconds.
    /// </summary>
    public int StatsUpdateIntervalMs { get; set; } = 1000;
}

/// <summary>
/// Application theme mode.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark,

    /// <summary>
    /// Follow system theme.
    /// </summary>
    Auto,
}

/// <summary>
/// Logging level.
/// </summary>
public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error,
}
