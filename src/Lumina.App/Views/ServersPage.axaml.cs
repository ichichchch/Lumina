namespace Lumina.App.Views;

public partial class ServersPage : UserControl
{
    public ServersPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ServersViewModel>();
    }
}
