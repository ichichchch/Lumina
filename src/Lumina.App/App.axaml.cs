namespace Lumina.App;

using Lumina.App.Localization;

/// <summary>
/// Avalonia 应用程序对象，负责应用级初始化、依赖注入配置与主窗口创建。
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 应用的根 <see cref="IServiceProvider"/>。
    /// </summary>
    /// <remarks>
    /// 该属性在 <see cref="OnFrameworkInitializationCompleted"/> 中完成构建与赋值，
    /// 供 Views 等位置解析 ViewModel/服务依赖。
    /// </remarks>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// 初始化应用资源（加载 App.axaml）。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 当框架初始化完成时，配置依赖注入并创建主窗口。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        // 配置依赖注入
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var localizationService = Services.GetRequiredService<ILocalizationService>();
        var settings = Services.GetRequiredService<IConfigurationStore>()
            .LoadSettingsAsync()
            .GetAwaiter()
            .GetResult();
        localizationService.SetCulture(settings.Language);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 注册应用所需的依赖注入服务与 ViewModel。
    /// </summary>
    /// <param name="services">服务集合。</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // 核心服务
        services.AddSingleton<IKeyStorage, DpapiKeyStorage>();
        services.AddSingleton<IKeyGenerator, Curve25519KeyGenerator>();
        services.AddSingleton<IConfigurationStore, JsonConfigurationStore>();
        services.AddSingleton<IRouteManager, WindowsRouteManager>();
        services.AddSingleton<IDnsManager, WindowsDnsManager>();
        services.AddSingleton<IDriverManager, WireGuardDriverManager>();
        services.AddSingleton<IWireGuardDriver, WireGuardDriver>();
        services.AddSingleton<IVpnService, VpnService>();

        // 应用服务
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();

        // 视图模型
        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ServersViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AddServerViewModel>();
    }
}
