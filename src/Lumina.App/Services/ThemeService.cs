namespace Lumina.App.Services;

using Avalonia.Platform;

/// <summary>
/// 主题服务接口。
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// 当前主题变体。
    /// </summary>
    ThemeVariant CurrentTheme { get; }

    /// <summary>
    /// 当主题发生变化时触发的事件。
    /// </summary>
    event EventHandler<ThemeVariant>? ThemeChanged;

    /// <summary>
    /// 设置主题。
    /// </summary>
    /// <param name="theme">要设置的主题。</param>
    void SetTheme(ThemeVariant theme);

    /// <summary>
    /// 在浅色与深色主题之间切换。
    /// </summary>
    void ToggleTheme();
}

/// <summary>
/// 主题服务实现。
/// </summary>
public sealed class ThemeService : IThemeService
{
    private ThemeVariant _requestedTheme = ThemeVariant.Dark;
    private ThemeVariant _effectiveTheme = ThemeVariant.Dark;
    private IPlatformSettings? _platformSettings;

    /// <inheritdoc />
    public ThemeVariant CurrentTheme => _effectiveTheme;

    /// <inheritdoc />
    public event EventHandler<ThemeVariant>? ThemeChanged;

    /// <inheritdoc />
    public void SetTheme(ThemeVariant theme)
    {
        if (_requestedTheme == theme)
            return;

        _requestedTheme = theme;

        if (Application.Current is not null)
        {
            if (theme == ThemeVariant.Default)
            {
                StartFollowSystemTheme(Application.Current);
            }
            else
            {
                StopFollowSystemTheme();
                Application.Current.RequestedThemeVariant = theme;
                UpdateEffectiveTheme(theme);
            }
        }
    }

    /// <inheritdoc />
    public void ToggleTheme()
    {
        SetTheme(_effectiveTheme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark);
    }

    private void StartFollowSystemTheme(Application app)
    {
        var platformSettings = app.PlatformSettings;
        if (platformSettings is null)
        {
            app.RequestedThemeVariant = ThemeVariant.Dark;
            UpdateEffectiveTheme(ThemeVariant.Dark);
            return;
        }

        if (!ReferenceEquals(_platformSettings, platformSettings))
        {
            StopFollowSystemTheme();
            _platformSettings = platformSettings;
            _platformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;
        }

        var requested = ResolveSystemThemeVariant(platformSettings.GetColorValues());
        app.RequestedThemeVariant = requested;
        UpdateEffectiveTheme(requested);
    }

    private void StopFollowSystemTheme()
    {
        if (_platformSettings is null)
            return;

        _platformSettings.ColorValuesChanged -= OnPlatformColorValuesChanged;
        _platformSettings = null;
    }

    private void OnPlatformColorValuesChanged(object? sender, PlatformColorValues e)
    {
        if (_requestedTheme != ThemeVariant.Default)
            return;

        if (Application.Current is null)
            return;

        var requested = ResolveSystemThemeVariant(e);
        Application.Current.RequestedThemeVariant = requested;
        UpdateEffectiveTheme(requested);
    }

    private void UpdateEffectiveTheme(ThemeVariant theme)
    {
        if (_effectiveTheme == theme)
            return;

        _effectiveTheme = theme;
        ThemeChanged?.Invoke(this, _effectiveTheme);
    }

    private static ThemeVariant ResolveSystemThemeVariant(PlatformColorValues? colorValues)
    {
        if (colorValues is null)
            return ThemeVariant.Dark;

        return colorValues.ThemeVariant switch
        {
            PlatformThemeVariant.Light => ThemeVariant.Light,
            PlatformThemeVariant.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Dark
        };
    }
}
