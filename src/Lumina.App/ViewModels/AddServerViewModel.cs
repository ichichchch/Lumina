namespace Lumina.App.ViewModels;

/// <summary>
/// 用于“添加服务器”页面的 ViewModel。
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
    /// 保存成功后触发的事件。
    /// </summary>
    public event EventHandler? SaveCompleted;

    /// <summary>
    /// 初始化 <see cref="AddServerViewModel"/>。
    /// </summary>
    /// <param name="configStore">配置存储实现，用于持久化隧道配置。</param>
    /// <param name="keyGenerator">密钥生成器，用于生成 WireGuard 密钥对。</param>
    /// <param name="navigationService">导航服务，用于在页面间切换。</param>
    public AddServerViewModel(
        IConfigurationStore configStore, 
        IKeyGenerator keyGenerator,
        INavigationService navigationService)
    {
        _configStore = configStore;
        _keyGenerator = keyGenerator;
        _navigationService = navigationService;
    }

    /// <summary>
    /// 获取当前表单是否满足“保存”条件。
    /// </summary>
    /// <remarks>
    /// 该属性用于控制 UI 中“保存”按钮的可用状态。
    /// </remarks>
    public bool CanSave =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(PublicKey) &&
        !IsSaving;

    /// <summary>
    /// 生成新的密钥对，并将私钥写入 <see cref="PrivateKey"/>。
    /// </summary>
    [RelayCommand]
    private void GenerateKeyPair()
    {
        var (privateKey, _) = _keyGenerator.GenerateKeyPair();
        PrivateKey = privateKey;
    }

    /// <summary>
    /// 校验并保存当前表单对应的隧道配置。
    /// </summary>
    /// <param name="cancellationToken">用于取消保存操作的令牌。</param>
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
            
            // 重置表单并返回上一页
            ResetForm();
            _navigationService.NavigateTo("Servers");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 将表单字段重置为默认值。
    /// </summary>
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

    /// <summary>
    /// 取消添加并返回服务器列表页。
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        // 返回服务器列表页
        _navigationService.NavigateTo("Servers");
    }

    /// <summary>
    /// 根据当前表单字段构建 <see cref="TunnelConfiguration"/>。
    /// </summary>
    /// <returns>由表单字段组成的隧道配置。</returns>
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
