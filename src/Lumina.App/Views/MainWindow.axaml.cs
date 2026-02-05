using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Lumina.App.Localization;
using Microsoft.Extensions.Logging;

namespace Lumina.App.Views;

/// <summary>
/// 应用主窗口。
/// </summary>
public partial class MainWindow : Window
{
    private readonly IConfigurationStore _configStore;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MainWindow> _logger;
    private TrayIcon? _trayIcon;
    private bool _allowClose;
    private bool _isPromptingClose;
    private DateTime _lastTrayClickUtc;
    private CloseAction _closeActionPreference = CloseAction.MinimizeToTray;
    private bool _closeConfirmSkip;

    /// <summary>
    /// 初始化主窗口并加载 XAML 组件。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        _configStore = App.Services.GetRequiredService<IConfigurationStore>();
        _localizationService = App.Services.GetRequiredService<ILocalizationService>();
        _logger = App.Services.GetRequiredService<ILogger<MainWindow>>();
        Closing += OnWindowClosing;
        Closed += OnWindowClosed;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadClosePreferenceAsync();
        InitializeTrayIcon();
        LogEnvironmentInfo("InitializeAsync");
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

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        LogCloseEvent("OnWindowClosing_Start");
        if (_allowClose)
        {
            _logger.LogInformation("T{ThreadId} Close allowed without interception", Environment.CurrentManagedThreadId);
            return;
        }

        // 必须在任何 await 之前立即取消，否则窗口会在异步操作完成前关闭
        e.Cancel = true;

        await LoadClosePreferenceAsync();

        if (_closeConfirmSkip)
        {
            if (_closeActionPreference == CloseAction.Exit)
            {
                _logger.LogInformation("T{ThreadId} Close preference=Exit, skip confirm=true", Environment.CurrentManagedThreadId);
                _allowClose = true;
                Close();
                return;
            }

            _logger.LogInformation("T{ThreadId} Close preference=MinimizeToTray, skip confirm=true", Environment.CurrentManagedThreadId);
            MinimizeToTray();
            return;
        }

        if (_isPromptingClose)
        {
            return;
        }

        _isPromptingClose = true;
        var result = await ShowCloseConfirmDialogAsync();
        _isPromptingClose = false;

        if (result is null)
        {
            return;
        }

        if (result.Action == CloseAction.Exit)
        {
            await SaveClosePreferenceAsync(CloseAction.Exit, result.Skip);
            _logger.LogInformation("T{ThreadId} Close confirmed: Exit, skip={Skip}", Environment.CurrentManagedThreadId, result.Skip);
            _allowClose = true;
            Close();
            return;
        }

        await SaveClosePreferenceAsync(CloseAction.MinimizeToTray, result.Skip);
        _logger.LogInformation("T{ThreadId} Close confirmed: MinimizeToTray, skip={Skip}", Environment.CurrentManagedThreadId, result.Skip);
        MinimizeToTray();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _logger.LogInformation("T{ThreadId} Window closed", Environment.CurrentManagedThreadId);
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private async Task LoadClosePreferenceAsync()
    {
        var settings = await _configStore.LoadSettingsAsync();
        _closeActionPreference = settings.CloseAction;
        _closeConfirmSkip = settings.CloseConfirmSkip;
        _logger.LogInformation(
            "T{ThreadId} Loaded close preference: action={Action}, skipConfirm={Skip}",
            Environment.CurrentManagedThreadId,
            _closeActionPreference,
            _closeConfirmSkip);
    }

    private async Task SaveClosePreferenceAsync(CloseAction action, bool? skip = null)
    {
        _closeActionPreference = action;
        var settings = await _configStore.LoadSettingsAsync();
        settings.CloseAction = action;
        if (skip.HasValue)
        {
            settings.CloseConfirmSkip = skip.Value;
            _closeConfirmSkip = skip.Value;
        }
        await _configStore.SaveSettingsAsync(settings);
    }

    private async Task<CloseDialogResult?> ShowCloseConfirmDialogAsync()
    {
        var title = _localizationService["CloseConfirm_Title"];
        var message = _localizationService["CloseConfirm_Message"];
        var minimizeText = _localizationService["CloseConfirm_Minimize"];
        var exitText = _localizationService["CloseConfirm_Exit"];
        var doNotAskText = _localizationService["CloseConfirm_DoNotAsk"];

        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var closeButton = new Button
        {
            Classes = { "WindowControlButton", "CloseButton" },
            Content = new PathIcon
            {
                Data = Geometry.Parse("M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12 19 6.41z"),
                Width = 12,
                Height = 12
            }
        };

        var minimizeButton = new Button
        {
            Content = new TextBlock { Text = minimizeText },
            MinWidth = 140
        };

        var exitButton = new Button
        {
            Content = new TextBlock { Text = exitText },
            MinWidth = 120
        };

        if (_closeActionPreference == CloseAction.MinimizeToTray)
        {
            minimizeButton.Classes.Add("Primary");
            minimizeButton.IsDefault = true;
            exitButton.Classes.Add("Secondary");
        }
        else
        {
            exitButton.Classes.Add("Primary");
            exitButton.IsDefault = true;
            minimizeButton.Classes.Add("Secondary");
        }

        var doNotAskAgain = new CheckBox
        {
            Classes = { "Lumina" },
            Content = doNotAskText
        };

        closeButton.Click += (_, _) => dialog.Close();
        minimizeButton.Click += (_, _) => dialog.Close(new CloseDialogResult(CloseAction.MinimizeToTray, doNotAskAgain.IsChecked == true));
        exitButton.Click += (_, _) => dialog.Close(new CloseDialogResult(CloseAction.Exit, doNotAskAgain.IsChecked == true));

        dialog.Content = new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(24),
            Children =
            {
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            Classes = { "Title" }
                        },
                        CreateDialogCloseContainer(closeButton)
                    }
                },
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Classes = { "Body" },
                    MaxWidth = 420
                },
                doNotAskAgain,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 12,
                    Children = { minimizeButton, exitButton }
                }
            }
        };

        return await dialog.ShowDialog<CloseDialogResult?>(this);
    }

    private void InitializeTrayIcon()
    {
        if (Application.Current is null)
        {
            _logger.LogWarning("T{ThreadId} Tray icon init skipped: Application.Current is null", Environment.CurrentManagedThreadId);
            return;
        }

        try
        {
            var menu = new NativeMenu();
            var openItem = new NativeMenuItem { Header = _localizationService["Tray_Open"] };
            openItem.Click += (_, _) => RestoreFromTray();
            var exitItem = new NativeMenuItem { Header = _localizationService["Tray_Exit"] };
            exitItem.Click += (_, _) => ExitFromTray();
            menu.Items.Add(openItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exitItem);

            _trayIcon = new TrayIcon
            {
                Icon = CreateTrayIcon(),
                ToolTipText = _localizationService["Tray_Tooltip"],
                IsVisible = true,
                Command = new RelayCommand(HandleTrayCommand),
                Menu = menu
            };

            TrayIcon.SetIcons(Application.Current, new TrayIcons { _trayIcon });
            _logger.LogInformation(
                "T{ThreadId} Tray icon initialized: visible={Visible}, hasIcon={HasIcon}, hasMenu={HasMenu}",
                Environment.CurrentManagedThreadId,
                _trayIcon.IsVisible,
                _trayIcon.Icon is not null,
                _trayIcon.Menu is not null);
        }
        catch (Exception ex)
        {
            _trayIcon = null;
            _logger.LogError(ex, "T{ThreadId} Tray icon initialization failed", Environment.CurrentManagedThreadId);
        }
    }

    private WindowIcon CreateTrayIcon()
    {
        var size = new PixelSize(32, 32);
        var bitmap = new RenderTargetBitmap(size);
        using (var context = bitmap.CreateDrawingContext())
        {
            var rect = new Rect(0, 0, 32, 32);
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#4C7CFF")), null, rect, 8, 8);
            context.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(16, 16), 6, 6);
        }

        return new WindowIcon(bitmap);
    }

    private void HandleTrayCommand()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastTrayClickUtc;
        _lastTrayClickUtc = now;

        if (elapsed.TotalMilliseconds <= 400)
        {
            RestoreFromTray();
        }
    }

    private void MinimizeToTray()
    {
        if (_trayIcon is null)
        {
            _logger.LogWarning("T{ThreadId} Tray icon missing, retrying initialization", Environment.CurrentManagedThreadId);
            InitializeTrayIcon();
        }

        if (_trayIcon is null)
        {
            _logger.LogWarning("T{ThreadId} Tray icon still missing, fallback to taskbar minimize. Check notification area permissions or security software rules.", Environment.CurrentManagedThreadId);
            WindowState = WindowState.Minimized;
            return;
        }

        _logger.LogInformation("T{ThreadId} Minimize to tray", Environment.CurrentManagedThreadId);
        ShowInTaskbar = false;
        WindowState = WindowState.Minimized;
        Hide();
    }

    private void RestoreFromTray()
    {
        _logger.LogInformation("T{ThreadId} Restore from tray", Environment.CurrentManagedThreadId);
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Show();
        Activate();
    }

    private void ExitFromTray()
    {
        _logger.LogInformation("T{ThreadId} Exit from tray menu", Environment.CurrentManagedThreadId);
        _allowClose = true;
        Close();
    }

    private static Border CreateDialogCloseContainer(Control closeButton)
    {
        var container = new Border
        {
            Child = closeButton
        };
        Grid.SetColumn(container, 1);
        return container;
    }

    private sealed record CloseDialogResult(CloseAction Action, bool Skip);

    private void LogCloseEvent(string stage)
    {
        _logger.LogInformation(
            "T{ThreadId} {Stage} action={Action} skipConfirm={Skip} allowClose={AllowClose}",
            Environment.CurrentManagedThreadId,
            stage,
            _closeActionPreference,
            _closeConfirmSkip,
            _allowClose);
    }

    private void LogEnvironmentInfo(string stage)
    {
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        var osVersion = RuntimeInformation.OSDescription;
        _logger.LogInformation(
            "T{ThreadId} {Stage} appVersion={AppVersion} os={OsVersion}",
            Environment.CurrentManagedThreadId,
            stage,
            appVersion,
            osVersion);
    }
}
