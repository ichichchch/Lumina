namespace Lumina.App.ViewModels;

/// <summary>
/// ViewModel for adding a new server.
/// </summary>
public partial class AddServerViewModel : ViewModelBase
{
    private readonly IConfigurationStore _configStore;
    private readonly IKeyGenerator _keyGenerator;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _endpoint = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _publicKey = string.Empty;

    [ObservableProperty]
    private string _presharedKey = string.Empty;

    [ObservableProperty]
    private string _allowedIPs = "0.0.0.0/0, ::/0";

    [ObservableProperty]
    private string _dnsServers = "1.1.1.1, 1.0.0.1";

    [ObservableProperty]
    private string _interfaceAddress = "10.0.0.2/32";

    [ObservableProperty]
    private ushort _persistentKeepalive = 25;

    [ObservableProperty]
    private string _privateKey = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _validationErrors = [];

    [ObservableProperty]
    private bool _isSaving;

    /// <summary>
    /// Event raised when save completes successfully.
    /// </summary>
    public event EventHandler? SaveCompleted;

    public AddServerViewModel(
        IConfigurationStore configStore, 
        IKeyGenerator keyGenerator,
        INavigationService navigationService)
    {
        _configStore = configStore;
        _keyGenerator = keyGenerator;
        _navigationService = navigationService;
    }

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(PublicKey) &&
        !IsSaving;

    [RelayCommand]
    private void GenerateKeyPair()
    {
        var (privateKey, _) = _keyGenerator.GenerateKeyPair();
        PrivateKey = privateKey;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ValidationErrors.Clear();

        var config = BuildConfiguration();
        var errors = config.Validate();

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                ValidationErrors.Add(error);
            }
            return;
        }

        IsSaving = true;

        try
        {
            await _configStore.SaveConfigurationAsync(config, cancellationToken);
            SaveCompleted?.Invoke(this, EventArgs.Empty);
            
            // Reset form and navigate back
            ResetForm();
            _navigationService.NavigateTo("Servers");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ResetForm()
    {
        Name = string.Empty;
        Location = string.Empty;
        Endpoint = string.Empty;
        PublicKey = string.Empty;
        PresharedKey = string.Empty;
        AllowedIPs = "0.0.0.0/0, ::/0";
        DnsServers = "1.1.1.1, 1.0.0.1";
        InterfaceAddress = "10.0.0.2/32";
        PersistentKeepalive = 25;
        PrivateKey = string.Empty;
        ValidationErrors.Clear();
    }

    [RelayCommand]
    private void Cancel()
    {
        // Navigate back to Servers page
        _navigationService.NavigateTo("Servers");
    }

    private TunnelConfiguration BuildConfiguration()
    {
        var allowedIpList = AllowedIPs
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        var dnsList = DnsServers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        var addressList = InterfaceAddress
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        return new TunnelConfiguration
        {
            Name = Name.Trim(),
            Location = string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(),
            PrivateKey = PrivateKey,
            Addresses = addressList,
            DnsServers = dnsList,
            Peers =
            [
                new PeerConfiguration
                {
                    PublicKey = PublicKey.Trim(),
                    PresharedKey = string.IsNullOrWhiteSpace(PresharedKey) ? null : PresharedKey.Trim(),
                    Endpoint = Endpoint.Trim(),
                    AllowedIPs = allowedIpList,
                    PersistentKeepalive = PersistentKeepalive,
                }
            ]
        };
    }
}
