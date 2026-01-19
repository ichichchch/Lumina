namespace Lumina.App.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationStore _configStore;
    private readonly IThemeService _themeService;

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
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string? _driverVersion;

    public string[] ThemeOptions { get; } = ["Light", "Dark", "Auto"];

    public SettingsViewModel(IConfigurationStore configStore, IThemeService themeService)
    {
        _configStore = configStore;
        _themeService = themeService;

        _ = LoadSettingsAsync();
    }

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
    }

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
            }
        };

        await _configStore.SaveSettingsAsync(settings);
    }

    [RelayCommand]
    private void ExportLogs()
    {
        // TODO: Implement log export
    }

    [RelayCommand]
    private void CheckForUpdates()
    {
        // TODO: Implement update check
    }
}
