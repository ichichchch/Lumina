using Avalonia;
using Avalonia.Styling;

namespace Lumina.App.Services;

/// <summary>
/// Theme service interface.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Current theme variant.
    /// </summary>
    ThemeVariant CurrentTheme { get; }

    /// <summary>
    /// Event raised when theme changes.
    /// </summary>
    event EventHandler<ThemeVariant>? ThemeChanged;

    /// <summary>
    /// Sets the theme.
    /// </summary>
    /// <param name="theme">Theme to set.</param>
    void SetTheme(ThemeVariant theme);

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    void ToggleTheme();
}

/// <summary>
/// Theme service implementation.
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
