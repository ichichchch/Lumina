namespace Lumina.Core.Configuration;

/// <summary>
/// 基于 JSON 的配置存储实现：使用 DPAPI 对敏感数据进行保护。
/// 通过 System.Text.Json 源生成器提供 AOT 兼容的序列化支持。
/// </summary>
public sealed class JsonConfigurationStore : IConfigurationStore
{
    private readonly string _configDirectory;
    private readonly IKeyStorage _keyStorage;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ConfigsSubdirectory = "Configs";
    private const string SettingsFileName = "settings.json";

    /// <summary>
    /// 创建一个新的 JSON 配置存储实例。
    /// </summary>
    /// <param name="keyStorage">私钥存储实现。</param>
    /// <param name="baseDirectory">
    /// 配置存储的基目录；默认为 %LOCALAPPDATA%\Lumina。
    /// </param>
    public JsonConfigurationStore(IKeyStorage keyStorage, string? baseDirectory = null)
    {
        _keyStorage = keyStorage;
        _configDirectory = baseDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lumina");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = LuminaJsonContext.Default,
        };
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(TunnelConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        EnsureDirectoryExists();

        // 使用 DPAPI 将私钥单独存储（避免明文落盘）
        if (!string.IsNullOrWhiteSpace(configuration.PrivateKey))
        {
            var keyIdentifier = configuration.Id.ToString();
            var keyBytes = Convert.FromBase64String(configuration.PrivateKey);
            await _keyStorage.StorePrivateKeyAsync(keyIdentifier, keyBytes, cancellationToken);
            configuration.PrivateKeyRef = keyIdentifier;
        }

        configuration.ModifiedAt = DateTimeOffset.UtcNow;

        var filePath = GetConfigFilePath(configuration.Id);
        var json = JsonSerializer.Serialize(configuration, LuminaJsonContext.Default.TunnelConfiguration);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TunnelConfiguration?> LoadConfigurationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filePath = GetConfigFilePath(id);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var configuration = JsonSerializer.Deserialize(json, LuminaJsonContext.Default.TunnelConfiguration);

        if (configuration is not null)
        {
            await LoadPrivateKeyAsync(configuration, cancellationToken);
        }

        return configuration;
    }

    /// <inheritdoc />
    public async Task<TunnelConfiguration?> LoadConfigurationByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var configs = await LoadAllConfigurationsAsync(cancellationToken);
        return configs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<List<TunnelConfiguration>> LoadAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var configsDir = GetConfigsDirectory();

        if (!Directory.Exists(configsDir))
        {
            return [];
        }

        var configurations = new List<TunnelConfiguration>();
        var files = Directory.GetFiles(configsDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var configuration = JsonSerializer.Deserialize(json, LuminaJsonContext.Default.TunnelConfiguration);

                if (configuration is not null)
                {
                    await LoadPrivateKeyAsync(configuration, cancellationToken);
                    configurations.Add(configuration);
                }
            }
            catch (JsonException)
            {
                // 跳过损坏的配置文件
            }
        }

        return configurations.OrderBy(c => c.Name).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteConfigurationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // 删除私钥
        await _keyStorage.DeletePrivateKeyAsync(id.ToString(), cancellationToken);

        // 删除配置文件
        var filePath = GetConfigFilePath(id);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        EnsureDirectoryExists();

        var filePath = Path.Combine(_configDirectory, SettingsFileName);
        var json = JsonSerializer.Serialize(settings, LuminaJsonContext.Default.AppSettings);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_configDirectory, SettingsFileName);

        if (!File.Exists(filePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize(json, LuminaJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// 如果配置包含私钥引用，则从 <see cref="IKeyStorage"/> 加载私钥并回填到配置对象。
    /// </summary>
    /// <param name="configuration">要回填私钥的配置对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步加载的任务。</returns>
    private async Task LoadPrivateKeyAsync(TunnelConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(configuration.PrivateKeyRef))
        {
            var keyBytes = await _keyStorage.LoadPrivateKeyAsync(configuration.PrivateKeyRef, cancellationToken);
            if (keyBytes is not null)
            {
                configuration.PrivateKey = Convert.ToBase64String(keyBytes);
            }
        }
    }

    /// <summary>
    /// 获取隧道配置文件存放目录路径。
    /// </summary>
    /// <returns>配置文件目录路径。</returns>
    private string GetConfigsDirectory()
    {
        return Path.Combine(_configDirectory, ConfigsSubdirectory);
    }

    /// <summary>
    /// 获取指定配置 ID 对应的配置文件路径。
    /// </summary>
    /// <param name="id">配置 ID。</param>
    /// <returns>配置文件完整路径。</returns>
    private string GetConfigFilePath(Guid id)
    {
        return Path.Combine(GetConfigsDirectory(), $"{id}.json");
    }

    /// <summary>
    /// 确保存储目录存在；如果不存在则创建。
    /// </summary>
    private void EnsureDirectoryExists()
    {
        var configsDir = GetConfigsDirectory();
        if (!Directory.Exists(configsDir))
        {
            Directory.CreateDirectory(configsDir);
        }
    }
}

/// <summary>
/// 用于 AOT 兼容的 JSON 序列化上下文。
/// </summary>
[JsonSerializable(typeof(TunnelConfiguration))]
[JsonSerializable(typeof(List<TunnelConfiguration>))]
[JsonSerializable(typeof(PeerConfiguration))]
[JsonSerializable(typeof(List<PeerConfiguration>))]
[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class LuminaJsonContext : JsonSerializerContext
{
}
