using Avalonia.Controls;
using Lumina.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lumina.App.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();
    }
}
