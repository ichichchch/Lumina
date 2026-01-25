namespace Lumina.App.Services;

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
    private ThemeVariant _currentTheme = ThemeVariant.Dark;

    /// <inheritdoc />
    public ThemeVariant CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public event EventHandler<ThemeVariant>? ThemeChanged;

    /// <inheritdoc />
    public void SetTheme(ThemeVariant theme)
    {
        if (_currentTheme == theme)
            return;

        _currentTheme = theme;

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = theme;
        }

        ThemeChanged?.Invoke(this, theme);
    }

    /// <inheritdoc />
    public void ToggleTheme()
    {
        SetTheme(_currentTheme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark);
    }
}
