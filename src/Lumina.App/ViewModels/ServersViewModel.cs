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

    [ObservableProperty]
    private ObservableCollection<ServerItemViewModel> _servers = [];

    [ObservableProperty]
    private ObservableCollection<ServerItemViewModel> _favoriteServers = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ServerItemViewModel? _selectedServer;

    /// <summary>
    /// 初始化 <see cref="ServersViewModel"/>，并触发加载服务器列表。
    /// </summary>
    /// <param name="configStore">配置存储，用于读取/写入服务器配置。</param>
    /// <param name="vpnService">VPN 服务，用于连接/断开。</param>
    /// <param name="navigationService">导航服务，用于跳转到添加服务器页面。</param>
    public ServersViewModel(
        IConfigurationStore configStore, 
        IVpnService vpnService,
        INavigationService navigationService)
    {
        _configStore = configStore;
        _vpnService = vpnService;
        _navigationService = navigationService;

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

        Servers = new ObservableCollection<ServerItemViewModel>(allServers.Where(s => !s.IsFavorite));
        FavoriteServers = new ObservableCollection<ServerItemViewModel>(allServers.Where(s => s.IsFavorite));
    }

    /// <summary>
    /// 连接到指定服务器；如果当前已连接，则先断开再连接。
    /// </summary>
    /// <param name="server">要连接的服务器条目。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    [RelayCommand]
    private async Task ConnectToServerAsync(ServerItemViewModel server, CancellationToken cancellationToken)
    {
        if (_vpnService.CurrentState == ConnectionState.Connected)
        {
            await _vpnService.DisconnectAsync(cancellationToken);
        }

        await _vpnService.ConnectAsync(server.Configuration, cancellationToken);
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
        await _configStore.DeleteConfigurationAsync(server.Configuration.Id);
        await LoadServersAsync();
    }

    /// <summary>
    /// 导航到“添加服务器”页面。
    /// </summary>
    [RelayCommand]
    private void AddServer()
    {
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
