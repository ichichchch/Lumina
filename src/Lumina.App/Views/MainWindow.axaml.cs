namespace Lumina.App.Views;

/// <summary>
/// 应用主窗口。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 初始化主窗口并加载 XAML 组件。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 处理鼠标指针按下事件，用于在自定义标题栏区域触发窗口拖拽。
    /// </summary>
    /// <param name="e">指针按下事件参数。</param>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        var point = e.GetCurrentPoint(this);
        var position = point.Position;
        
        // 允许在标题栏区域（顶部 40px）拖拽窗口
        // 但需排除右侧窗口控制按钮区域（最后 150px）
        if (position.Y < 40 && position.X < (Bounds.Width - 150))
        {
            BeginMoveDrag(e);
        }
    }

    /// <summary>
    /// 最小化窗口。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">路由事件参数。</param>
    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// 在最大化与普通状态之间切换窗口。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">路由事件参数。</param>
    private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }

    /// <summary>
    /// 关闭窗口。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">路由事件参数。</param>
    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
