using Lumina.App.Localization;

namespace Lumina.App.ViewModels;

/// <summary>
/// 设置页的 ViewModel，负责加载/保存应用设置并切换主题。
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationStore _configStore;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private bool _autoConnect;

    [ObservableProperty]
    private bool _startOnBoot;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _killSwitch;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedLanguageIndex;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string? _driverVersion;

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
        ILocalizationService localizationService)
    {
        _configStore = configStore;
        _themeService = themeService;
        _localizationService = localizationService;

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
    /// 从配置存储加载应用设置，并同步到可绑定属性。
    /// </summary>
    /// <returns>表示异步加载的任务。</returns>
    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        var settings = await _configStore.LoadSettingsAsync();

        AutoConnect = settings.AutoConnect;
        StartOnBoot = settings.StartOnBoot;
        StartMinimized = settings.StartMinimized;
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

        OnPropertyChanged(nameof(DriverVersionDisplay));
    }

    /// <summary>
    /// 将当前可绑定属性组装为 <see cref="AppSettings"/> 并持久化。
    /// </summary>
    /// <returns>表示异步保存的任务。</returns>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            AutoConnect = AutoConnect,
            StartOnBoot = StartOnBoot,
            StartMinimized = StartMinimized,
            KillSwitch = KillSwitch,
            Theme = SelectedThemeIndex switch
            {
                0 => ThemeMode.Light,
                1 => ThemeMode.Dark,
                _ => ThemeMode.Auto
            },
            Language = SelectedLanguageIndex switch
            {
                2 => "zh-CN",
                1 => "en-US",
                _ => null
            }
        };

        await _configStore.SaveSettingsAsync(settings);
    }

    /// <summary>
    /// 导出日志。
    /// </summary>
    [RelayCommand]
    private void ExportLogs()
    {
        // TODO: 实现日志导出
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
