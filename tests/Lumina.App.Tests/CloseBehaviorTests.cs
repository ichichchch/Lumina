namespace Lumina.App.Tests;

public sealed class CloseBehaviorTests
{
    [Fact]
    public void 设置页应提供关闭行为下拉选项()
    {
        var settingsPage = ReadRepoFile("src", "Lumina.App", "Views", "SettingsPage.axaml");

        Assert.Contains("x:Name=\"CloseBehaviorComboBox\"", settingsPage, StringComparison.Ordinal);
        Assert.Contains("SelectedCloseBehaviorIndex", settingsPage, StringComparison.Ordinal);
        Assert.Contains("Settings_CloseBehaviorMinimize", settingsPage, StringComparison.Ordinal);
        Assert.Contains("Settings_CloseBehaviorExit", settingsPage, StringComparison.Ordinal);
    }

    [Fact]
    public void 关闭行为应覆盖最小化与退出()
    {
        var mainWindow = ReadRepoFile("src", "Lumina.App", "Views", "MainWindow.axaml.cs");

        Assert.Contains("_closeActionPreference == CloseAction.Exit", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MinimizeToTray();", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Close();", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void 关闭行为应具备中英文文案()
    {
        var stringsEn = ReadRepoFile("src", "Lumina.App", "Resources", "Strings", "Strings.en.axaml");
        var stringsZh = ReadRepoFile("src", "Lumina.App", "Resources", "Strings", "Strings.zh-CN.axaml");

        Assert.Contains("Settings_CloseBehaviorTitle", stringsEn, StringComparison.Ordinal);
        Assert.Contains("Settings_CloseBehaviorDesc", stringsEn, StringComparison.Ordinal);
        Assert.Contains("Settings_CloseBehaviorMinimize", stringsEn, StringComparison.Ordinal);
        Assert.Contains("Settings_CloseBehaviorExit", stringsEn, StringComparison.Ordinal);

        Assert.Contains("Settings_CloseBehaviorTitle", stringsZh, StringComparison.Ordinal);
        Assert.Contains("Settings_CloseBehaviorDesc", stringsZh, StringComparison.Ordinal);
        Assert.Contains("Settings_CloseBehaviorMinimize", stringsZh, StringComparison.Ordinal);
        Assert.Contains("Settings_CloseBehaviorExit", stringsZh, StringComparison.Ordinal);
    }

    [Fact]
    public void 托盘菜单应具备中英文文案()
    {
        var stringsEn = ReadRepoFile("src", "Lumina.App", "Resources", "Strings", "Strings.en.axaml");
        var stringsZh = ReadRepoFile("src", "Lumina.App", "Resources", "Strings", "Strings.zh-CN.axaml");

        Assert.Contains("Tray_Open", stringsEn, StringComparison.Ordinal);
        Assert.Contains("Tray_Exit", stringsEn, StringComparison.Ordinal);
        Assert.Contains("Tray_Open", stringsZh, StringComparison.Ordinal);
        Assert.Contains("Tray_Exit", stringsZh, StringComparison.Ordinal);
    }

    [Fact]
    public void 主窗口应初始化托盘菜单与退出入口()
    {
        var mainWindow = ReadRepoFile("src", "Lumina.App", "Views", "MainWindow.axaml.cs");

        Assert.Contains("NativeMenu", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Tray_Open", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Tray_Exit", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ExitFromTray", mainWindow, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativePathSegments)
    {
        var root = FindRepositoryRoot();
        var fullPath = Path.Combine(root.FullName, Path.Combine(relativePathSegments));
        return File.ReadAllText(fullPath);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Lumina.sln")))
                return current;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("未能定位仓库根目录（缺少 Lumina.sln）。");
    }
}
