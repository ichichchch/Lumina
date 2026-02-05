using Lumina.App.Localization;
using Lumina.App.Services;

namespace Lumina.App.ViewModels;

/// <summary>
/// 设置页的 ViewModel，负责加载/保存应用设置并切换主题。
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationStore _configStore;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogExportService _logExportService;
    private bool _isLoading;

    [ObservableProperty]
    private bool _autoConnect;

    [ObservableProperty]
    private bool _startOnBoot;

    [ObservableProperty]
    private bool _killSwitch;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedLanguageIndex;

    [ObservableProperty]
    private int _selectedCloseBehaviorIndex;

    [ObservableProperty]
    private bool _closeConfirmEnabled;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string? _driverVersion;

    [ObservableProperty]
    private DateTimeOffset? _exportStartDate;

    [ObservableProperty]
    private DateTimeOffset? _exportEndDate;

    public string DriverVersionDisplay =>
        string.IsNullOrWhiteSpace(DriverVersion) ? LocalizationService.Instance["Settings_NotInstalled"] : DriverVersion;

    /// <summary>
    /// 初始化 <see cref="SettingsViewModel"/> 并加载当前设置。
    /// </summary>
    /// <param name="configStore">配置存储，用于读取/写入应用设置。</param>
    /// <param name="themeService">主题服务，用于设置应用主题。</param>
    /// <param name="localizationService">本地化服务，用于设置界面语言。</param>
    public SettingsViewModel(
        IConfigurationStore configStore,
        IThemeService themeService,
        ILocalizationService localizationService,
        ILogExportService logExportService)
    {
        _configStore = configStore;
        _themeService = themeService;
        _localizationService = localizationService;
        _logExportService = logExportService;

        var now = DateTimeOffset.Now;
        ExportStartDate = new DateTimeOffset(now.Date, now.Offset);
        ExportEndDate = new DateTimeOffset(now.Date, now.Offset);

        _ = LoadSettingsAsync();
    }

    /// <summary>
    /// 当所选主题索引变化时，将其映射为 <see cref="Avalonia.Styling.ThemeVariant"/> 并应用。
    /// </summary>
    /// <param name="value">新的主题索引。</param>
    partial void OnSelectedThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            0 => Avalonia.Styling.ThemeVariant.Light,
            1 => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default
        };
        _themeService.SetTheme(theme);
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        var cultureName = value switch
        {
            2 => "zh-CN",
            1 => "en-US",
            _ => null
        };
        _localizationService.SetCulture(cultureName);
        OnPropertyChanged(nameof(DriverVersionDisplay));
    }

    /// <summary>
    /// 当所选关闭行为索引变化时，立即保存设置。
    /// </summary>
    /// <param name="value">新的关闭行为索引。</param>
    partial void OnSelectedCloseBehaviorIndexChanged(int value)
    {
        if (_isLoading) return;
        _ = SaveCloseBehaviorAsync(value);
    }

    private async Task SaveCloseBehaviorAsync(int index)
    {
        if (index < 0) return;
        
        var settings = await _configStore.LoadSettingsAsync();
        settings.CloseAction = index == 1 ? CloseAction.Exit : CloseAction.MinimizeToTray;
        await _configStore.SaveSettingsAsync(settings);
    }

    /// <summary>
    /// 当关闭确认开关变化时，立即保存设置。
    /// </summary>
    /// <param name="value">是否启用关闭确认。</param>
    partial void OnCloseConfirmEnabledChanged(bool value)
    {
        if (_isLoading) return;
        _ = SaveCloseConfirmAsync(value);
    }

    private async Task SaveCloseConfirmAsync(bool enabled)
    {
        var settings = await _configStore.LoadSettingsAsync();
        settings.CloseConfirmSkip = !enabled;
        await _configStore.SaveSettingsAsync(settings);
    }

    /// <summary>
    /// 从配置存储加载应用设置，并同步到可绑定属性。
    /// </summary>
    /// <returns>表示异步加载的任务。</returns>
    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        _isLoading = true;
        try
        {
            var settings = await _configStore.LoadSettingsAsync();

            AutoConnect = settings.AutoConnect;
            StartOnBoot = settings.StartOnBoot;
            KillSwitch = settings.KillSwitch;

            SelectedThemeIndex = settings.Theme switch
            {
                ThemeMode.Light => 0,
                ThemeMode.Dark => 1,
                _ => 2
            };

            SelectedLanguageIndex = string.IsNullOrWhiteSpace(settings.Language)
                ? 0
                : settings.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? 2 : 1;

            SelectedCloseBehaviorIndex = settings.CloseAction == CloseAction.Exit ? 1 : 0;

            CloseConfirmEnabled = !settings.CloseConfirmSkip;

            OnPropertyChanged(nameof(DriverVersionDisplay));
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// 将当前可绑定属性组装为 <see cref="AppSettings"/> 并持久化。
    /// </summary>
    /// <returns>表示异步保存的任务。</returns>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = await _configStore.LoadSettingsAsync();
        settings.AutoConnect = AutoConnect;
        settings.StartOnBoot = StartOnBoot;
        settings.KillSwitch = KillSwitch;
        if (SelectedCloseBehaviorIndex >= 0)
        {
            settings.CloseAction = SelectedCloseBehaviorIndex == 1 ? CloseAction.Exit : CloseAction.MinimizeToTray;
        }
        settings.Theme = SelectedThemeIndex switch
        {
            0 => ThemeMode.Light,
            1 => ThemeMode.Dark,
            _ => ThemeMode.Auto
        };
        settings.Language = SelectedLanguageIndex switch
        {
            2 => "zh-CN",
            1 => "en-US",
            _ => null
        };

        await _configStore.SaveSettingsAsync(settings);
    }

    /// <summary>
    /// 导出日志。
    /// </summary>
    [RelayCommand]
    private async Task ExportLogs()
    {
        await _logExportService.ExportLogsAsync(ExportStartDate, ExportEndDate);
    }

    /// <summary>
    /// 检查更新。
    /// </summary>
    [RelayCommand]
    private void CheckForUpdates()
    {
        // TODO: 实现更新检查
    }
}
