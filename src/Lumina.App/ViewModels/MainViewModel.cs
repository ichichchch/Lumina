namespace Lumina.App.ViewModels;

using System.ComponentModel;
using Avalonia.Styling;
using Lumina.App.Localization;
using Lumina.App.Services;

/// <summary>
/// 主窗口的 ViewModel：负责导航、驱动初始化、配置加载以及连接状态展示。
/// </summary>
public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IVpnService _vpnService;
    private readonly IConfigurationStore _configStore;
    private readonly INavigationService _navigationService;
    private readonly IDriverManager _driverManager;
    private readonly IThemeService _themeService;
    private readonly IDisposable _stateSubscription;
    private readonly IDisposable _statsSubscription;
    private readonly PropertyChangedEventHandler _localizationChangedHandler;
    private readonly EventHandler<ThemeVariant> _themeChangedHandler;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    [NotifyPropertyChangedFor(nameof(IsConnecting))]
    [NotifyPropertyChangedFor(nameof(IsDisconnected))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    [NotifyPropertyChangedFor(nameof(CanConnect))]
    [NotifyPropertyChangedFor(nameof(CanToggleConnection))]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private TrafficStats _currentStats = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedServerNameText))]
    [NotifyPropertyChangedFor(nameof(SelectedServerLocationText))]
    private TunnelConfiguration? _selectedConfiguration;

    [ObservableProperty]
    private ObservableCollection<TunnelConfiguration> _configurations = [];

    [ObservableProperty]
    private string _currentPage = "Home";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConnect))]
    [NotifyPropertyChangedFor(nameof(CanToggleConnection))]
    private bool _isDriverInstalled;

    [ObservableProperty]
    private string? _driverVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggleConnection))]
    private bool _isInitializingDriver;

    [ObservableProperty]
    private string? _driverError;

    /// <summary>
    /// 初始化 <see cref="MainViewModel"/> 并建立状态/统计/导航订阅。
    /// </summary>
    /// <param name="vpnService">VPN 服务，用于连接/断开并提供状态与统计流。</param>
    /// <param name="configStore">配置存储，用于加载隧道配置列表。</param>
    /// <param name="navigationService">导航服务，用于在页面间切换。</param>
    /// <param name="driverManager">驱动管理器，用于检查与安装 WireGuard 驱动。</param>
    public MainViewModel(
        IVpnService vpnService,
        IConfigurationStore configStore,
        INavigationService navigationService,
        IDriverManager driverManager,
        IThemeService themeService)
    {
        _vpnService = vpnService;
        _configStore = configStore;
        _navigationService = navigationService;
        _driverManager = driverManager;
        _themeService = themeService;

        // 订阅连接状态变化
        _stateSubscription = _vpnService.ConnectionStateStream
            .Subscribe(state => ConnectionState = state);

        // 订阅流量统计
        _statsSubscription = _vpnService.TrafficStatsStream
            .Sample(TimeSpan.FromMilliseconds(100))
            .Subscribe(stats => CurrentStats = stats);

        // 订阅导航事件
        _navigationService.Navigated += (_, page) => CurrentPage = page;

        _localizationChangedHandler = (_, __) =>
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(SelectedServerNameText));
            OnPropertyChanged(nameof(SelectedServerLocationText));
            OnPropertyChanged(nameof(TotalUploadText));
            OnPropertyChanged(nameof(TotalDownloadText));
            OnPropertyChanged(nameof(HeroTitle));
        };
        LocalizationService.Instance.PropertyChanged += _localizationChangedHandler;

        _themeChangedHandler = (_, __) => OnPropertyChanged(nameof(HeroTitle));
        _themeService.ThemeChanged += _themeChangedHandler;

        // 初始化驱动并加载配置
        _ = InitializeAsync();
    }

    /// <summary>
    /// 初始化流程：确保驱动可用并加载配置列表。
    /// </summary>
    /// <returns>表示异步初始化的任务。</returns>
    private async Task InitializeAsync()
    {
        // 初始化驱动
        await EnsureDriverReadyAsync();
        
        // 加载配置
        await LoadConfigurationsAsync();
    }

    /// <summary>
    /// 确保 WireGuard 驱动已就绪，并更新驱动状态相关属性。
    /// </summary>
    /// <returns>表示异步驱动初始化的任务。</returns>
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
                DriverVersion = version?.ToString() ?? LocalizationService.Instance["Common_Installed"];
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
    /// 根据连接状态获取状态文本。
    /// </summary>
    public string StatusText => ConnectionState switch
    {
        ConnectionState.Disconnected => LocalizationService.Instance["Status_Disconnected"],
        ConnectionState.Connecting => LocalizationService.Instance["Status_Connecting"],
        ConnectionState.Connected => LocalizationService.Instance["Status_Connected"],
        ConnectionState.Disconnecting => LocalizationService.Instance["Status_Disconnecting"],
        ConnectionState.Error => LocalizationService.Instance["Status_Error"],
        _ => LocalizationService.Instance["Common_Unknown"]
    };

    public string HeroTitle =>
        _themeService.CurrentTheme == ThemeVariant.Light
            ? LocalizationService.Instance["Home_HeroTitleLight"]
            : LocalizationService.Instance["Home_HeroTitleDark"];

    public string SelectedServerNameText =>
        SelectedConfiguration?.Name ?? LocalizationService.Instance["Home_NoServerSelected"];

    public string SelectedServerLocationText =>
        SelectedConfiguration?.Location ?? LocalizationService.Instance["Home_SelectServerHint"];

    /// <summary>
    /// 获取当前是否已连接。
    /// </summary>
    public bool IsConnected => ConnectionState == ConnectionState.Connected;

    public bool IsConnecting => ConnectionState is ConnectionState.Connecting or ConnectionState.Disconnecting;

    public bool IsDisconnected => ConnectionState == ConnectionState.Disconnected;

    public bool IsError => ConnectionState == ConnectionState.Error;

    /// <summary>
    /// 获取“连接”操作当前是否可用。
    /// </summary>
    public bool CanConnect => ConnectionState == ConnectionState.Disconnected && SelectedConfiguration is not null && IsDriverInstalled;

    public bool CanToggleConnection => !IsInitializingDriver && (IsConnected || CanConnect);

    /// <summary>
    /// 格式化后的上传速率文本。
    /// </summary>
    public string UploadSpeed => TrafficStats.FormatSpeed(CurrentStats.TxBytesPerSecond);

    /// <summary>
    /// 格式化后的下载速率文本。
    /// </summary>
    public string DownloadSpeed => TrafficStats.FormatSpeed(CurrentStats.RxBytesPerSecond);

    /// <summary>
    /// 格式化后的累计上传流量文本。
    /// </summary>
    public string TotalUpload => TrafficStats.FormatBytes(CurrentStats.TxBytes);

    /// <summary>
    /// 格式化后的累计下载流量文本。
    /// </summary>
    public string TotalDownload => TrafficStats.FormatBytes(CurrentStats.RxBytes);

    public string TotalUploadText => string.Format(LocalizationService.Instance["Home_TotalFormat"], TotalUpload);

    public string TotalDownloadText => string.Format(LocalizationService.Instance["Home_TotalFormat"], TotalDownload);

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
        OnPropertyChanged(nameof(TotalUploadText));
        OnPropertyChanged(nameof(TotalDownloadText));
    }

    /// <summary>
    /// 当所选配置变更时，通知 <see cref="CanConnect"/> 更新。
    /// </summary>
    /// <param name="value">新的所选隧道配置。</param>
    partial void OnSelectedConfigurationChanged(TunnelConfiguration? value)
    {
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(CanToggleConnection));
    }

    /// <summary>
    /// 根据当前连接状态执行连接或断开操作。
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
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

    /// <summary>
    /// 断开当前连接。
    /// </summary>
    /// <param name="cancellationToken">用于取消断开操作的令牌。</param>
    [RelayCommand]
    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await _vpnService.DisconnectAsync(cancellationToken);
    }

    /// <summary>
    /// 导航到指定页面。
    /// </summary>
    /// <param name="page">页面标识。</param>
    [RelayCommand]
    private void NavigateTo(string page)
    {
        _navigationService.NavigateTo(page);
    }

    /// <summary>
    /// 从配置存储加载全部隧道配置，并设置默认选中项。
    /// </summary>
    /// <returns>表示异步加载的任务。</returns>
    [RelayCommand]
    private async Task LoadConfigurationsAsync()
    {
        var selectedId = SelectedConfiguration?.Id;
        var configs = await _configStore.LoadAllConfigurationsAsync();
        Configurations = new ObservableCollection<TunnelConfiguration>(configs);

        var matched = selectedId.HasValue
            ? Configurations.FirstOrDefault(c => c.Id == selectedId.Value)
            : null;

        SelectedConfiguration = matched
            ?? Configurations.FirstOrDefault(c => c.IsFavorite)
            ?? Configurations.FirstOrDefault();
    }

    public Task RefreshConfigurationsAsync() => LoadConfigurationsAsync();

    /// <summary>
    /// 选中指定的隧道配置。
    /// </summary>
    /// <param name="configuration">要选中的配置。</param>
    [RelayCommand]
    private void SelectConfiguration(TunnelConfiguration configuration)
    {
        SelectedConfiguration = configuration;
    }

    /// <summary>
    /// 释放订阅资源并抑制终结器。
    /// </summary>
    public void Dispose()
    {
        LocalizationService.Instance.PropertyChanged -= _localizationChangedHandler;
        _themeService.ThemeChanged -= _themeChangedHandler;
        _stateSubscription.Dispose();
        _statsSubscription.Dispose();
        GC.SuppressFinalize(this);
    }
}
