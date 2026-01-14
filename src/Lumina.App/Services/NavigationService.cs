namespace Lumina.App.Services;

/// <summary>
/// Navigation service interface for switching between views.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Current page name.
    /// </summary>
    string CurrentPage { get; }

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event EventHandler<string>? Navigated;

    /// <summary>
    /// Navigates to the specified page.
    /// </summary>
    /// <param name="pageName">Name of the page to navigate to.</param>
    void NavigateTo(string pageName);
}

/// <summary>
/// Navigation service implementation.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private string _currentPage = "Home";

    /// <inheritdoc />
    public string CurrentPage => _currentPage;

    /// <inheritdoc />
    public event EventHandler<string>? Navigated;

    /// <inheritdoc />
    public void NavigateTo(string pageName)
    {
        if (_currentPage == pageName)
            return;

        _currentPage = pageName;
        Navigated?.Invoke(this, pageName);
    }
}
