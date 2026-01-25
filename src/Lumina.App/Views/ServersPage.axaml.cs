namespace Lumina.App.Views;

/// <summary>
/// 服务器列表视图。
/// </summary>
public partial class ServersPage : UserControl
{
    /// <summary>
    /// 初始化服务器列表视图并加载 XAML 组件，同时解析并绑定对应的 ViewModel。
    /// </summary>
    public ServersPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ServersViewModel>();
    }
}
