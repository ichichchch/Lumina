namespace Lumina.App.ViewModels;

/// <summary>
/// ViewModel for the Servers page.
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

    partial void OnSearchQueryChanged(string value)
    {
        FilterServers();
    }

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

    [RelayCommand]
    private async Task ConnectToServerAsync(ServerItemViewModel server, CancellationToken cancellationToken)
    {
        if (_vpnService.CurrentState == ConnectionState.Connected)
        {
            await _vpnService.DisconnectAsync(cancellationToken);
        }

        await _vpnService.ConnectAsync(server.Configuration, cancellationToken);
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(ServerItemViewModel server)
    {
        server.IsFavorite = !server.IsFavorite;
        server.Configuration.IsFavorite = server.IsFavorite;

        await _configStore.SaveConfigurationAsync(server.Configuration);
        await LoadServersAsync();
    }

    [RelayCommand]
    private async Task DeleteServerAsync(ServerItemViewModel server)
    {
        await _configStore.DeleteConfigurationAsync(server.Configuration.Id);
        await LoadServersAsync();
    }

    [RelayCommand]
    private void AddServer()
    {
        _navigationService.NavigateTo("AddServer");
    }

    private void FilterServers()
    {
        // Re-filter based on search query
        // This is simplified - in production use a filtered view
    }
}

/// <summary>
/// ViewModel for a server list item.
/// </summary>
public partial class ServerItemViewModel : ViewModelBase
{
    public TunnelConfiguration Configuration { get; }

    [ObservableProperty]
    private bool _isFavorite;

    public ServerItemViewModel(TunnelConfiguration configuration)
    {
        Configuration = configuration;
        IsFavorite = configuration.IsFavorite;
    }

    public string Name => Configuration.Name;
    public string Location => Configuration.Location ?? "Unknown";
    public string Endpoint => Configuration.PrimaryEndpoint ?? "-";
    public int? LatencyMs => Configuration.LatencyMs;
    public string LatencyText => LatencyMs.HasValue ? $"{LatencyMs}ms" : "-";
}
