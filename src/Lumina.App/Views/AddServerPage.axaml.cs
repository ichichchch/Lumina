namespace Lumina.App.Views;

public partial class AddServerPage : UserControl
{
    public AddServerPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<AddServerViewModel>();
    }
}
