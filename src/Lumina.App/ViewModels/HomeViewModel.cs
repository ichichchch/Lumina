namespace Lumina.App.ViewModels;

/// <summary>
/// ViewModel for the Home page.
/// </summary>
public partial class HomeViewModel : ViewModelBase, IDisposable
{
    private readonly IVpnService _vpnService;
    private readonly IDisposable _stateSubscription;
    private readonly IDisposable _statsSubscription;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    [NotifyPropertyChangedFor(nameof(IsConnecting))]
    [NotifyPropertyChangedFor(nameof(ConnectionButtonText))]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private TrafficStats _currentStats = new();

    [ObservableProperty]
    private TunnelConfiguration? _currentConfiguration;

    public HomeViewModel(IVpnService vpnService)
    {
        _vpnService = vpnService;

        _stateSubscription = _vpnService.ConnectionStateStream
            .Subscribe(state => ConnectionState = state);

        _statsSubscription = _vpnService.TrafficStatsStream
            .Sample(TimeSpan.FromMilliseconds(100))
            .Subscribe(stats => CurrentStats = stats);

        CurrentConfiguration = _vpnService.CurrentConfiguration;
    }

    public string StatusText => ConnectionState switch
    {
        ConnectionState.Disconnected => "Tap to connect",
        ConnectionState.Connecting => "Connecting...",
        ConnectionState.Connected => "Connected",
        ConnectionState.Disconnecting => "Disconnecting...",
        ConnectionState.Error => "Connection error",
        _ => "Unknown"
    };

    public bool IsConnected => ConnectionState == ConnectionState.Connected;
    public bool IsConnecting => ConnectionState == ConnectionState.Connecting;

    public string ConnectionButtonText => IsConnected ? "Disconnect" : "Connect";

    public string UploadSpeed => TrafficStats.FormatSpeed(CurrentStats.TxBytesPerSecond);
    public string DownloadSpeed => TrafficStats.FormatSpeed(CurrentStats.RxBytesPerSecond);
    public string TotalUpload => TrafficStats.FormatBytes(CurrentStats.TxBytes);
    public string TotalDownload => TrafficStats.FormatBytes(CurrentStats.RxBytes);

    public string ServerName => CurrentConfiguration?.Name ?? "No server selected";
    public string ServerLocation => CurrentConfiguration?.Location ?? "Unknown";
    public string ServerEndpoint => CurrentConfiguration?.PrimaryEndpoint ?? "-";

    partial void OnCurrentStatsChanged(TrafficStats value)
    {
        OnPropertyChanged(nameof(UploadSpeed));
        OnPropertyChanged(nameof(DownloadSpeed));
        OnPropertyChanged(nameof(TotalUpload));
        OnPropertyChanged(nameof(TotalDownload));
    }

    partial void OnCurrentConfigurationChanged(TunnelConfiguration? value)
    {
        OnPropertyChanged(nameof(ServerName));
        OnPropertyChanged(nameof(ServerLocation));
        OnPropertyChanged(nameof(ServerEndpoint));
    }

    [RelayCommand]
    private async Task ToggleConnectionAsync(CancellationToken cancellationToken)
    {
        if (IsConnected || IsConnecting)
        {
            await _vpnService.DisconnectAsync(cancellationToken);
        }
        else if (CurrentConfiguration is not null)
        {
            await _vpnService.ConnectAsync(CurrentConfiguration, cancellationToken);
        }
    }

    public void Dispose()
    {
        _stateSubscription.Dispose();
        _statsSubscription.Dispose();
        GC.SuppressFinalize(this);
    }
}
