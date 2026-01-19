namespace Lumina.App.ViewModels;

/// <summary>
/// Main window ViewModel - manages navigation and overall app state.
/// </summary>
public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IVpnService _vpnService;
    private readonly IConfigurationStore _configStore;
    private readonly INavigationService _navigationService;
    private readonly IDriverManager _driverManager;
    private readonly IDisposable _stateSubscription;
    private readonly IDisposable _statsSubscription;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    [NotifyPropertyChangedFor(nameof(CanConnect))]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private TrafficStats _currentStats = new();

    [ObservableProperty]
    private TunnelConfiguration? _selectedConfiguration;

    [ObservableProperty]
    private ObservableCollection<TunnelConfiguration> _configurations = [];

    [ObservableProperty]
    private string _currentPage = "Home";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConnect))]
    private bool _isDriverInstalled;

    [ObservableProperty]
    private string? _driverVersion;

    [ObservableProperty]
    private bool _isInitializingDriver;

    [ObservableProperty]
    private string? _driverError;

    public MainViewModel(
        IVpnService vpnService,
        IConfigurationStore configStore,
        INavigationService navigationService,
        IDriverManager driverManager)
    {
        _vpnService = vpnService;
        _configStore = configStore;
        _navigationService = navigationService;
        _driverManager = driverManager;

        // Subscribe to connection state changes
        _stateSubscription = _vpnService.ConnectionStateStream
            .Subscribe(state => ConnectionState = state);

        // Subscribe to traffic stats
        _statsSubscription = _vpnService.TrafficStatsStream
            .Sample(TimeSpan.FromMilliseconds(100))
            .Subscribe(stats => CurrentStats = stats);

        // Subscribe to navigation
        _navigationService.Navigated += (_, page) => CurrentPage = page;

        // Initialize driver and load configurations
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Initialize driver
        await EnsureDriverReadyAsync();
        
        // Load configurations
        await LoadConfigurationsAsync();
    }

    [RelayCommand]
    private async Task EnsureDriverReadyAsync()
    {
        IsInitializingDriver = true;
        DriverError = null;

        try
        {
            var result = await _driverManager.EnsureDriverReadyAsync();
            
            if (result.Success)
            {
                IsDriverInstalled = true;
                var version = _driverManager.GetInstalledDriverVersion();
                DriverVersion = version?.ToString() ?? "Installed";
            }
            else
            {
                IsDriverInstalled = false;
                DriverError = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            IsDriverInstalled = false;
            DriverError = ex.Message;
        }
        finally
        {
            IsInitializingDriver = false;
        }
    }

    /// <summary>
    /// Gets the status text based on connection state.
    /// </summary>
    public string StatusText => ConnectionState switch
    {
        ConnectionState.Disconnected => "Disconnected",
        ConnectionState.Connecting => "Connecting...",
        ConnectionState.Connected => "Connected",
        ConnectionState.Disconnecting => "Disconnecting...",
        ConnectionState.Error => "Error",
        _ => "Unknown"
    };

    /// <summary>
    /// Whether currently connected.
    /// </summary>
    public bool IsConnected => ConnectionState == ConnectionState.Connected;

    /// <summary>
    /// Whether connect action is available.
    /// </summary>
    public bool CanConnect => ConnectionState == ConnectionState.Disconnected && SelectedConfiguration is not null && IsDriverInstalled;

    /// <summary>
    /// Formatted upload speed.
    /// </summary>
    public string UploadSpeed => TrafficStats.FormatSpeed(CurrentStats.TxBytesPerSecond);

    /// <summary>
    /// Formatted download speed.
    /// </summary>
    public string DownloadSpeed => TrafficStats.FormatSpeed(CurrentStats.RxBytesPerSecond);

    /// <summary>
    /// Formatted total upload.
    /// </summary>
    public string TotalUpload => TrafficStats.FormatBytes(CurrentStats.TxBytes);

    /// <summary>
    /// Formatted total download.
    /// </summary>
    public string TotalDownload => TrafficStats.FormatBytes(CurrentStats.RxBytes);

    partial void OnCurrentStatsChanged(TrafficStats value)
    {
        OnPropertyChanged(nameof(UploadSpeed));
        OnPropertyChanged(nameof(DownloadSpeed));
        OnPropertyChanged(nameof(TotalUpload));
        OnPropertyChanged(nameof(TotalDownload));
    }

    partial void OnSelectedConfigurationChanged(TunnelConfiguration? value)
    {
        OnPropertyChanged(nameof(CanConnect));
    }

    [RelayCommand]
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (SelectedConfiguration is null)
            return;

        if (IsConnected)
        {
            await _vpnService.DisconnectAsync(cancellationToken);
        }
        else
        {
            await _vpnService.ConnectAsync(SelectedConfiguration, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await _vpnService.DisconnectAsync(cancellationToken);
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        _navigationService.NavigateTo(page);
    }

    [RelayCommand]
    private async Task LoadConfigurationsAsync()
    {
        var configs = await _configStore.LoadAllConfigurationsAsync();
        Configurations = new ObservableCollection<TunnelConfiguration>(configs);

        // Select favorite or first configuration
        SelectedConfiguration = Configurations.FirstOrDefault(c => c.IsFavorite)
            ?? Configurations.FirstOrDefault();
    }

    [RelayCommand]
    private void SelectConfiguration(TunnelConfiguration configuration)
    {
        SelectedConfiguration = configuration;
    }

    /// <summary>
    /// Disposes the ViewModel.
    /// </summary>
    public void Dispose()
    {
        _stateSubscription.Dispose();
        _statsSubscription.Dispose();
        GC.SuppressFinalize(this);
    }
}
