namespace Lumina.App.Tests;

public sealed class LanguageSwitchBorderAdaptiveTests
{
    [Fact]
    public void 语言切换时下拉框宽度应随文案变化自适应()
    {
        var settingsPage = ReadRepoFile("src", "Lumina.App", "Views", "SettingsPage.axaml");

        Assert.DoesNotContain("SelectedLanguageIndex\" Width=\"160\"", settingsPage, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"LanguageComboBox\"", settingsPage, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"160\"", settingsPage, StringComparison.Ordinal);
        Assert.Contains("MaxWidth=\"240\"", settingsPage, StringComparison.Ordinal);
    }

    [Fact]
    public void 下拉框应满足最小宽度避免短文本显得过窄()
    {
        var stringsEn = ReadRepoFile("src", "Lumina.App", "Resources", "Strings", "Strings.en.axaml");
        var stringsZh = ReadRepoFile("src", "Lumina.App", "Resources", "Strings", "Strings.zh-CN.axaml");

        var enZhCn = ExtractStringResourceValue(stringsEn, "Language_ZhCn");
        var zhZhCn = ExtractStringResourceValue(stringsZh, "Language_ZhCn");

        Assert.True(enZhCn.Length >= zhZhCn.Length, $"期望英文环境下的语言名不短于中文环境：en='{enZhCn}', zh='{zhZhCn}'");
    }

    [Fact]
    public void 焦点边框样式应与控件圆角一致()
    {
        var controls = ReadRepoFile("src", "Lumina.App", "Styles", "Controls.axaml");

        Assert.Contains("Style Selector=\"ComboBox.Lumina\"", controls, StringComparison.Ordinal);
        Assert.Contains("<FocusAdornerTemplate>", controls, StringComparison.Ordinal);
        Assert.Contains("BorderThickness=\"2\"", controls, StringComparison.Ordinal);
        Assert.Contains("CornerRadius=\"{StaticResource RadiusMedium}\"", controls, StringComparison.Ordinal);
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

    private static string ExtractStringResourceValue(string axaml, string key)
    {
        var marker = $"x:Key=\"{key}\">";
        var start = axaml.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidOperationException($"未找到字符串资源：{key}");

        start += marker.Length;
        var end = axaml.IndexOf("</x:String>", start, StringComparison.Ordinal);
        if (end < 0)
            throw new InvalidOperationException($"字符串资源格式不符合预期：{key}");

        return axaml[start..end].Trim();
    }
}
