using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumina.App.Services;
using Lumina.Core.Configuration;
using Lumina.Core.Models;
using Lumina.Core.Services;

namespace Lumina.App.ViewModels;

/// <summary>
/// Main window ViewModel - manages navigation and overall app state.
/// </summary>
public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IVpnService _vpnService;
    private readonly IConfigurationStore _configStore;
    private readonly INavigationService _navigationService;
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
    private bool _isDriverInstalled;

    [ObservableProperty]
    private string? _driverVersion;

    public MainViewModel(
        IVpnService vpnService,
        IConfigurationStore configStore,
        INavigationService navigationService)
    {
        _vpnService = vpnService;
        _configStore = configStore;
        _navigationService = navigationService;

        // Subscribe to connection state changes
        _stateSubscription = _vpnService.ConnectionStateStream
            .Subscribe(state => ConnectionState = state);

        // Subscribe to traffic stats
        _statsSubscription = _vpnService.TrafficStatsStream
            .Sample(TimeSpan.FromMilliseconds(100))
            .Subscribe(stats => CurrentStats = stats);

        // Subscribe to navigation
        _navigationService.Navigated += (_, page) => CurrentPage = page;

        // Check driver status
        IsDriverInstalled = _vpnService.IsDriverInstalled();
        var version = _vpnService.GetDriverVersion();
        DriverVersion = version?.ToString();

        // Load configurations
        _ = LoadConfigurationsAsync();
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
    public bool CanConnect => ConnectionState == ConnectionState.Disconnected && SelectedConfiguration is not null;

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
