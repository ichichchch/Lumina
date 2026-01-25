namespace Lumina.App.Services;

/// <summary>
/// 导航服务接口，用于在不同页面/视图之间切换。
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 当前页面标识。
    /// </summary>
    string CurrentPage { get; }

    /// <summary>
    /// 当发生导航时触发的事件。
    /// </summary>
    event EventHandler<string>? Navigated;

    /// <summary>
    /// 导航到指定页面。
    /// </summary>
    /// <param name="pageName">要导航到的页面标识。</param>
    void NavigateTo(string pageName);
}

/// <summary>
/// 导航服务实现。
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
