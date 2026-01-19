namespace Lumina.Core.Configuration;

/// <summary>
/// JSON-based configuration storage with DPAPI encryption for sensitive data.
/// Uses System.Text.Json source generators for AOT compatibility.
/// </summary>
public sealed class JsonConfigurationStore : IConfigurationStore
{
    private readonly string _configDirectory;
    private readonly IKeyStorage _keyStorage;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ConfigsSubdirectory = "Configs";
    private const string SettingsFileName = "settings.json";

    /// <summary>
    /// Creates a new JSON configuration store.
    /// </summary>
    /// <param name="keyStorage">Key storage for private keys.</param>
    /// <param name="baseDirectory">
    /// Base directory for config storage. Defaults to %LOCALAPPDATA%\Lumina.
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

        // Store the private key separately using DPAPI
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
                // Skip corrupted config files
            }
        }

        return configurations.OrderBy(c => c.Name).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteConfigurationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Delete the private key
        await _keyStorage.DeletePrivateKeyAsync(id.ToString(), cancellationToken);

        // Delete the config file
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
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize(json, LuminaJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

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

    private string GetConfigsDirectory()
    {
        return Path.Combine(_configDirectory, ConfigsSubdirectory);
    }

    private string GetConfigFilePath(Guid id)
    {
        return Path.Combine(GetConfigsDirectory(), $"{id}.json");
    }

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
/// JSON serialization context for AOT compatibility.
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
