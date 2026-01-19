namespace Lumina.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IKeyStorage, DpapiKeyStorage>();
        services.AddSingleton<IKeyGenerator, Curve25519KeyGenerator>();
        services.AddSingleton<IConfigurationStore, JsonConfigurationStore>();
        services.AddSingleton<IRouteManager, WindowsRouteManager>();
        services.AddSingleton<IDnsManager, WindowsDnsManager>();
        services.AddSingleton<IDriverManager, WireGuardDriverManager>();
        services.AddSingleton<IWireGuardDriver, WireGuardDriver>();
        services.AddSingleton<IVpnService, VpnService>();

        // App services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ServersViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AddServerViewModel>();
    }
}
