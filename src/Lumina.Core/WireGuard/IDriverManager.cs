namespace Lumina.Core.WireGuard;

/// <summary>
/// Represents the installation state of the WireGuard driver.
/// </summary>
public enum DriverInstallState
{
    /// <summary>Driver is not installed.</summary>
    NotInstalled,

    /// <summary>Driver is installed but not running.</summary>
    Stopped,

    /// <summary>Driver is installed and running.</summary>
    Running,

    /// <summary>Driver is currently being installed.</summary>
    Installing,

    /// <summary>Driver installation failed.</summary>
    Error
}

/// <summary>
/// Result of a driver installation operation.
/// </summary>
public sealed class DriverInstallResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int ErrorCode { get; init; }
    public DriverInstallState State { get; init; }

    public static DriverInstallResult Succeeded(DriverInstallState state) => new()
    {
        Success = true,
        State = state
    };

    public static DriverInstallResult Failed(string message, int errorCode = 0) => new()
    {
        Success = false,
        ErrorMessage = message,
        ErrorCode = errorCode,
        State = DriverInstallState.Error
    };
}

/// <summary>
/// Manages the WireGuardNT driver lifecycle including installation, uninstallation, and status.
/// </summary>
public interface IDriverManager
{
    /// <summary>
    /// Gets the current installation state of the driver.
    /// </summary>
    DriverInstallState GetDriverState();

    /// <summary>
    /// Checks if the driver is installed and ready to use.
    /// </summary>
    bool IsDriverReady();

    /// <summary>
    /// Installs the WireGuardNT driver from embedded resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the installation operation.</returns>
    Task<DriverInstallResult> InstallDriverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls the WireGuardNT driver.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the uninstallation operation.</returns>
    Task<DriverInstallResult> UninstallDriverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the driver is installed and running.
    /// If not installed, attempts to install it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<DriverInstallResult> EnsureDriverReadyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path where driver files are stored.
    /// </summary>
    string GetDriverPath();

    /// <summary>
    /// Gets the version of the installed driver, if available.
    /// </summary>
    Version? GetInstalledDriverVersion();
}
