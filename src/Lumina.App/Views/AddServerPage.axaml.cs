namespace Lumina.App.Views;

/// <summary>
/// 添加服务器视图。
/// </summary>
public partial class AddServerPage : UserControl
{
    /// <summary>
    /// 初始化添加服务器视图并加载 XAML 组件，同时解析并绑定对应的 ViewModel。
    /// </summary>
    public AddServerPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<AddServerViewModel>();
    }
}
