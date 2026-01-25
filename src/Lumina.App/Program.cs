namespace Lumina.App;

/// <summary>
/// 应用程序入口点与 Avalonia 应用构建器的工厂。
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// 应用程序主入口。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    [STAThread]
    public static void Main(string[] args)
    {
        if (Lumina.Core.WireGuard.EmbeddableTunnelServiceEntry.TryRunFromServiceArgs(args, out var exitCode))
        {
            Environment.Exit(exitCode);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 构建并配置 Avalonia <see cref="AppBuilder"/>。
    /// </summary>
    /// <returns>已配置完成的 <see cref="AppBuilder"/>。</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
