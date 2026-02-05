using System.Collections.Specialized;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lumina.App.ViewModels;

using Lumina.App.Localization;

/// <summary>
/// 服务器列表页的 ViewModel，负责加载配置、连接服务器与收藏/删除等操作。
/// </summary>
public partial class ServersViewModel : ViewModelBase
{
    private readonly IConfigurationStore _configStore;
    private readonly IVpnService _vpnService;
    private readonly INavigationService _navigationService;
    private readonly AddServerViewModel _addServerViewModel;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private ObservableCollection<ServerItemViewModel> _servers = [];

    [ObservableProperty]
    private ObservableCollection<ServerItemViewModel> _favoriteServers = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ServerItemViewModel? _selectedServer;

    [ObservableProperty]
    private string? _connectError;

    /// <summary>
    /// 初始化 <see cref="ServersViewModel"/>，并触发加载服务器列表。
    /// </summary>
    /// <param name="configStore">配置存储，用于读取/写入服务器配置。</param>
    /// <param name="vpnService">VPN 服务，用于连接/断开。</param>
    /// <param name="navigationService">导航服务，用于跳转到添加服务器页面。</param>
    public ServersViewModel(
        IConfigurationStore configStore, 
        IVpnService vpnService,
        INavigationService navigationService,
        AddServerViewModel addServerViewModel,
        MainViewModel mainViewModel)
    {
        _configStore = configStore;
        _vpnService = vpnService;
        _navigationService = navigationService;
        _addServerViewModel = addServerViewModel;
        _mainViewModel = mainViewModel;

        Servers.CollectionChanged += OnServersCollectionChanged;
        FavoriteServers.CollectionChanged += OnFavoriteServersCollectionChanged;
        _navigationService.Navigated += OnNavigated;
        _addServerViewModel.SaveCompleted += OnSaveCompleted;

        _ = LoadServersAsync();
    }

    /// <summary>
    /// 当搜索关键词变化时触发过滤逻辑。
    /// </summary>
    /// <param name="value">新的搜索关键词。</param>
    partial void OnSearchQueryChanged(string value)
    {
        FilterServers();
    }

    /// <summary>
    /// 从配置存储加载全部服务器配置，并拆分为收藏与非收藏列表。
    /// </summary>
    /// <returns>表示异步加载的任务。</returns>
    [RelayCommand]
    private async Task LoadServersAsync()
    {
        var configs = await _configStore.LoadAllConfigurationsAsync();

        var allServers = configs
            .Select(c => new ServerItemViewModel(c))
            .ToList();

        Servers.Clear();
        foreach (var server in allServers.Where(s => !s.IsFavorite))
        {
            Servers.Add(server);
        }

        FavoriteServers.Clear();
        foreach (var server in allServers.Where(s => s.IsFavorite))
        {
            FavoriteServers.Add(server);
        }
    }

    /// <summary>
    /// 连接到指定服务器；如果当前已连接，则先断开再连接。
    /// </summary>
    /// <param name="server">要连接的服务器条目。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    [RelayCommand]
    private async Task ConnectToServerAsync(ServerItemViewModel server, CancellationToken cancellationToken)
    {
        ConnectError = null;

        try
        {
            if (_vpnService.CurrentState == ConnectionState.Connected)
            {
                await _vpnService.DisconnectAsync(cancellationToken);
            }

            await _vpnService.ConnectAsync(server.Configuration, cancellationToken);
        }
        catch (Exception ex)
        {
            ConnectError = ex.Message;
        }
    }

    /// <summary>
    /// 切换服务器的收藏状态并持久化，然后刷新列表。
    /// </summary>
    /// <param name="server">要切换收藏状态的服务器条目。</param>
    /// <returns>表示异步保存与刷新操作的任务。</returns>
    [RelayCommand]
    private async Task ToggleFavoriteAsync(ServerItemViewModel server)
    {
        server.IsFavorite = !server.IsFavorite;
        server.Configuration.IsFavorite = server.IsFavorite;

        await _configStore.SaveConfigurationAsync(server.Configuration);
        await LoadServersAsync();
    }

    /// <summary>
    /// 删除指定服务器配置并刷新列表。
    /// </summary>
    /// <param name="server">要删除的服务器条目。</param>
    /// <returns>表示异步删除与刷新操作的任务。</returns>
    [RelayCommand]
    private async Task DeleteServerAsync(ServerItemViewModel server)
    {
        if (!await ConfirmDeleteAsync(server))
            return;

        if (_vpnService.CurrentConfiguration?.Id == server.Configuration.Id &&
            _vpnService.CurrentState != ConnectionState.Disconnected)
        {
            await _vpnService.DisconnectAsync();
        }

        await _configStore.DeleteConfigurationAsync(server.Configuration.Id);
        await LoadServersAsync();
        await _mainViewModel.RefreshConfigurationsAsync();
    }

    private async Task<bool> ConfirmDeleteAsync(ServerItemViewModel server)
    {
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null)
            return false;

        var localization = LocalizationService.Instance;
        var title = localization["Servers_DeleteConfirmTitle"];
        var message = string.Format(localization["Servers_DeleteConfirmMessage"], server.Name);

        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var cancelButton = new Button
        {
            Content = new TextBlock { Text = localization["Common_Cancel"] },
            MinWidth = 96
        };
        cancelButton.Classes.Add("Secondary");
        cancelButton.Click += (_, _) => dialog.Close(false);

        var deleteButton = new Button
        {
            Content = new TextBlock { Text = localization["Servers_Delete"] },
            MinWidth = 96
        };
        deleteButton.Classes.Add("Primary");
        deleteButton.Click += (_, _) => dialog.Close(true);

        dialog.Content = new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(24),
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    Classes = { "Title" }
                },
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Classes = { "Body" },
                    MaxWidth = 420
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 12,
                    Children = { cancelButton, deleteButton }
                }
            }
        };

        return await dialog.ShowDialog<bool>(owner);
    }

    /// <summary>
    /// 导航到“添加服务器”页面。
    /// </summary>
    [RelayCommand]
    private void AddServer()
    {
        _addServerViewModel.StartNew();
        _navigationService.NavigateTo("AddServer");
    }

    [RelayCommand]
    private void EditServer(ServerItemViewModel server)
    {
        _addServerViewModel.StartEdit(server.Configuration);
        _navigationService.NavigateTo("AddServer");
    }

    /// <summary>
    /// 根据 <see cref="SearchQuery"/> 过滤服务器列表。
    /// </summary>
    private void FilterServers()
    {
        // 根据搜索词重新过滤
        // 这里为简化示例；生产环境建议使用过滤视图/集合视图
    }

    public bool HasServers => Servers.Count > 0;

    public bool HasFavoriteServers => FavoriteServers.Count > 0;

    public bool HasNoServers => Servers.Count == 0 && FavoriteServers.Count == 0;

    private void OnServersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasServers));
        OnPropertyChanged(nameof(HasNoServers));
    }

    private void OnFavoriteServersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasFavoriteServers));
        OnPropertyChanged(nameof(HasNoServers));
    }

    private void OnNavigated(object? sender, string page)
    {
        if (page == "Servers")
        {
            _ = LoadServersAsync();
        }
    }

    private void OnSaveCompleted(object? sender, EventArgs e)
    {
        _ = LoadServersAsync();
        _ = _mainViewModel.RefreshConfigurationsAsync();
    }
}

/// <summary>
/// 服务器列表项的 ViewModel。
/// </summary>
public partial class ServerItemViewModel : ViewModelBase
{
    public TunnelConfiguration Configuration { get; }

    [ObservableProperty]
    private bool _isFavorite;

    /// <summary>
    /// 初始化 <see cref="ServerItemViewModel"/>。
    /// </summary>
    /// <param name="configuration">对应的隧道配置。</param>
    public ServerItemViewModel(TunnelConfiguration configuration)
    {
        Configuration = configuration;
        IsFavorite = configuration.IsFavorite;
    }

    public string Name => Configuration.Name;
    public string Location => Configuration.Location ?? LocalizationService.Instance["Common_Unknown"];
    public string Endpoint => Configuration.PrimaryEndpoint ?? "-";
    public int? LatencyMs => Configuration.LatencyMs;
    public string LatencyText => LatencyMs.HasValue ? $"{LatencyMs}ms" : "-";
}
