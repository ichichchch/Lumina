namespace Lumina.App.ViewModels;

/// <summary>
/// 首页的 ViewModel，负责展示连接状态与流量统计，并触发连接/断开。
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

    /// <summary>
    /// 初始化 <see cref="HomeViewModel"/>。
    /// </summary>
    /// <param name="vpnService">VPN 服务，用于获取状态/统计并执行连接操作。</param>
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

    /// <summary>
    /// 当流量统计变更时，通知与速率/累计值相关的派生属性更新。
    /// </summary>
    /// <param name="value">新的流量统计。</param>
    partial void OnCurrentStatsChanged(TrafficStats value)
    {
        OnPropertyChanged(nameof(UploadSpeed));
        OnPropertyChanged(nameof(DownloadSpeed));
        OnPropertyChanged(nameof(TotalUpload));
        OnPropertyChanged(nameof(TotalDownload));
    }

    /// <summary>
    /// 当当前配置变更时，通知与服务器信息相关的派生属性更新。
    /// </summary>
    /// <param name="value">新的隧道配置。</param>
    partial void OnCurrentConfigurationChanged(TunnelConfiguration? value)
    {
        OnPropertyChanged(nameof(ServerName));
        OnPropertyChanged(nameof(ServerLocation));
        OnPropertyChanged(nameof(ServerEndpoint));
    }

    /// <summary>
    /// 根据当前状态切换连接：已连接/连接中则断开；有可用配置则连接。
    /// </summary>
    /// <param name="cancellationToken">用于取消连接/断开操作的令牌。</param>
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

    /// <summary>
    /// 释放订阅资源并抑制终结器。
    /// </summary>
    public void Dispose()
    {
        _stateSubscription.Dispose();
        _statsSubscription.Dispose();
        GC.SuppressFinalize(this);
    }
}
