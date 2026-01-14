using Lumina.Core.Models;

namespace Lumina.Core.Configuration;

/// <summary>
/// Interface for tunnel configuration storage.
/// </summary>
public interface IConfigurationStore
{
    /// <summary>
    /// Saves a tunnel configuration.
    /// </summary>
    /// <param name="configuration">Configuration to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveConfigurationAsync(TunnelConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a tunnel configuration by ID.
    /// </summary>
    /// <param name="id">Configuration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration, or null if not found.</returns>
    Task<TunnelConfiguration?> LoadConfigurationAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a tunnel configuration by name.
    /// </summary>
    /// <param name="name">Configuration name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration, or null if not found.</returns>
    Task<TunnelConfiguration?> LoadConfigurationByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all tunnel configurations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all configurations.</returns>
    Task<List<TunnelConfiguration>> LoadAllConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tunnel configuration.
    /// </summary>
    /// <param name="id">Configuration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteConfigurationAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves application settings.
    /// </summary>
    /// <param name="settings">Settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads application settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Application settings.</returns>
    Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);
}
