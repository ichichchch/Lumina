namespace Lumina.App.Views;

/// <summary>
/// 设置页视图。
/// </summary>
public partial class SettingsPage : UserControl
{
    /// <summary>
    /// 初始化设置页视图并加载 XAML 组件，同时解析并绑定对应的 ViewModel。
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();
    }
}
