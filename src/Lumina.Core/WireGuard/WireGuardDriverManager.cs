namespace Lumina.Core.WireGuard;

/// <summary>
/// Manages the WireGuardNT driver lifecycle including installation and uninstallation.
/// Driver files can be embedded as resources or provided externally.
/// </summary>
public sealed class WireGuardDriverManager : IDriverManager
{
    private const string ServiceName = "WireGuardTunnel$Lumina";
    private const string ServiceDisplayName = "Lumina WireGuard Tunnel";
    private const string DriverFileName = "wireguard.sys";
    private const string DllFileName = "wireguard.dll";

    private readonly string _driverDirectory;
    private readonly string? _externalDriverPath;
    private readonly object _installLock = new();

    /// <summary>
    /// Creates a new driver manager instance.
    /// </summary>
    /// <param name="driverDirectory">
    /// Directory to store/locate driver files. Defaults to %LOCALAPPDATA%\Lumina\Driver.
    /// </param>
    /// <param name="externalDriverPath">
    /// Optional path to externally provided driver files (for development without embedding).
    /// </param>
    public WireGuardDriverManager(string? driverDirectory = null, string? externalDriverPath = null)
    {
        _driverDirectory = driverDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lumina",
                "Driver");
        _externalDriverPath = externalDriverPath;
    }

    /// <summary>
    /// Gets the full path to wireguard.dll for P/Invoke loading.
    /// </summary>
    public string GetWireGuardDllPath()
    {
        return Path.Combine(_driverDirectory, DllFileName);
    }

    /// <inheritdoc />
    public DriverInstallState GetDriverState()
    {
        var scManager = DriverInstallNative.OpenSCManagerW(null, null, DriverInstallNative.SC_MANAGER_CONNECT);
        if (scManager == nint.Zero)
        {
            return DriverInstallState.Error;
        }

        try
        {
            var service = DriverInstallNative.OpenServiceW(
                scManager,
                ServiceName,
                DriverInstallNative.SERVICE_QUERY_STATUS);

            if (service == nint.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                return error == DriverInstallNative.ERROR_SERVICE_DOES_NOT_EXIST
                    ? DriverInstallState.NotInstalled
                    : DriverInstallState.Error;
            }

            try
            {
                if (DriverInstallNative.QueryServiceStatus(service, out var status))
                {
                    return status.dwCurrentState switch
                    {
                        DriverInstallNative.SERVICE_RUNNING => DriverInstallState.Running,
                        DriverInstallNative.SERVICE_STOPPED => DriverInstallState.Stopped,
                        DriverInstallNative.SERVICE_START_PENDING => DriverInstallState.Installing,
                        DriverInstallNative.SERVICE_STOP_PENDING => DriverInstallState.Stopped,
                        _ => DriverInstallState.Stopped
                    };
                }

                return DriverInstallState.Error;
            }
            finally
            {
                DriverInstallNative.CloseServiceHandle(service);
            }
        }
        finally
        {
            DriverInstallNative.CloseServiceHandle(scManager);
        }
    }

    /// <inheritdoc />
    public bool IsDriverReady()
    {
        var state = GetDriverState();
        return state is DriverInstallState.Running or DriverInstallState.Stopped;
    }

    /// <inheritdoc />
    public async Task<DriverInstallResult> InstallDriverAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => InstallDriverInternal(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DriverInstallResult> UninstallDriverAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => UninstallDriverInternal(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DriverInstallResult> EnsureDriverReadyAsync(CancellationToken cancellationToken = default)
    {
        var state = GetDriverState();

        switch (state)
        {
            case DriverInstallState.Running:
                return DriverInstallResult.Succeeded(DriverInstallState.Running);

            case DriverInstallState.Stopped:
                // Try to start the service
                return await Task.Run(() => StartDriverService(), cancellationToken);

            case DriverInstallState.NotInstalled:
                // Install and start
                var installResult = await InstallDriverAsync(cancellationToken);
                if (!installResult.Success)
                {
                    return installResult;
                }
                return await Task.Run(() => StartDriverService(), cancellationToken);

            default:
                return DriverInstallResult.Failed($"Driver is in unexpected state: {state}");
        }
    }

    /// <inheritdoc />
    public string GetDriverPath()
    {
        return _driverDirectory;
    }

    /// <inheritdoc />
    public Version? GetInstalledDriverVersion()
    {
        var dllPath = Path.Combine(_driverDirectory, DllFileName);
        if (!File.Exists(dllPath))
        {
            return null;
        }

        try
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(dllPath);
            if (Version.TryParse(versionInfo.FileVersion, out var version))
            {
                return version;
            }
        }
        catch
        {
            // Ignore version parse errors
        }

        return null;
    }

    private DriverInstallResult InstallDriverInternal()
    {
        lock (_installLock)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(_driverDirectory))
                {
                    Directory.CreateDirectory(_driverDirectory);
                }

                // Extract driver files from embedded resources
                ExtractDriverFiles();

                // Register the driver service
                return RegisterDriverService();
            }
            catch (Exception ex)
            {
                return DriverInstallResult.Failed($"Installation failed: {ex.Message}");
            }
        }
    }

    private void ExtractDriverFiles()
    {
        var sysTarget = Path.Combine(_driverDirectory, DriverFileName);
        var dllTarget = Path.Combine(_driverDirectory, DllFileName);

        // Check if files already exist and are valid
        if (File.Exists(sysTarget) && File.Exists(dllTarget))
        {
            return;
        }

        // Try to copy from external path first (development scenario)
        if (!string.IsNullOrEmpty(_externalDriverPath) && Directory.Exists(_externalDriverPath))
        {
            var externalSys = Path.Combine(_externalDriverPath, DriverFileName);
            var externalDll = Path.Combine(_externalDriverPath, DllFileName);

            if (File.Exists(externalSys) && File.Exists(externalDll))
            {
                File.Copy(externalSys, sysTarget, overwrite: true);
                File.Copy(externalDll, dllTarget, overwrite: true);
                return;
            }
        }

        // Try to extract from embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePrefix = "Lumina.Core.Resources.Driver.";

        var sysExtracted = TryExtractResource(assembly, $"{resourcePrefix}{DriverFileName}", sysTarget);
        var dllExtracted = TryExtractResource(assembly, $"{resourcePrefix}{DllFileName}", dllTarget);

        if (!sysExtracted || !dllExtracted)
        {
            throw new FileNotFoundException(
                "WireGuardNT driver files not found. " +
                "Either embed wireguard.sys and wireguard.dll as resources in Lumina.Core/Resources/Driver/, " +
                "or provide external driver path via configuration. " +
                $"Expected location: {_driverDirectory}");
        }
    }

    private static bool TryExtractResource(Assembly assembly, string resourceName, string targetPath)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return false;
        }

        using var fileStream = File.Create(targetPath);
        stream.CopyTo(fileStream);
        return true;
    }

    private static void ExtractResource(Assembly assembly, string resourceName, string targetPath)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            // Resource not embedded - this is acceptable during development
            // In production, the driver files should be embedded
            throw new FileNotFoundException(
                $"Driver resource '{resourceName}' not found. " +
                "Ensure driver files are embedded as resources or placed in the driver directory.");
        }

        using var fileStream = File.Create(targetPath);
        stream.CopyTo(fileStream);
    }

    private DriverInstallResult RegisterDriverService()
    {
        var driverPath = Path.Combine(_driverDirectory, DriverFileName);

        if (!File.Exists(driverPath))
        {
            return DriverInstallResult.Failed($"Driver file not found: {driverPath}");
        }

        var scManager = DriverInstallNative.OpenSCManagerW(
            null, null, DriverInstallNative.SC_MANAGER_ALL_ACCESS);

        if (scManager == nint.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            return DriverInstallResult.Failed(
                "Failed to open Service Control Manager. Ensure running as administrator.",
                error);
        }

        try
        {
            // Check if service already exists
            var existingService = DriverInstallNative.OpenServiceW(
                scManager, ServiceName, DriverInstallNative.SERVICE_ALL_ACCESS);

            if (existingService != nint.Zero)
            {
                DriverInstallNative.CloseServiceHandle(existingService);
                return DriverInstallResult.Succeeded(DriverInstallState.Stopped);
            }

            // Create the service
            var service = DriverInstallNative.CreateServiceW(
                scManager,
                ServiceName,
                ServiceDisplayName,
                DriverInstallNative.SERVICE_ALL_ACCESS,
                DriverInstallNative.SERVICE_KERNEL_DRIVER,
                DriverInstallNative.SERVICE_DEMAND_START,
                DriverInstallNative.SERVICE_ERROR_NORMAL,
                driverPath,
                null,
                nint.Zero,
                null,
                null,
                null);

            if (service == nint.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == DriverInstallNative.ERROR_SERVICE_EXISTS)
                {
                    return DriverInstallResult.Succeeded(DriverInstallState.Stopped);
                }

                return DriverInstallResult.Failed(
                    $"Failed to create driver service: {new Win32Exception(error).Message}",
                    error);
            }

            DriverInstallNative.CloseServiceHandle(service);
            return DriverInstallResult.Succeeded(DriverInstallState.Stopped);
        }
        finally
        {
            DriverInstallNative.CloseServiceHandle(scManager);
        }
    }

    private DriverInstallResult StartDriverService()
    {
        var scManager = DriverInstallNative.OpenSCManagerW(
            null, null, DriverInstallNative.SC_MANAGER_CONNECT);

        if (scManager == nint.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            return DriverInstallResult.Failed("Failed to open Service Control Manager", error);
        }

        try
        {
            var service = DriverInstallNative.OpenServiceW(
                scManager, ServiceName,
                DriverInstallNative.SERVICE_START | DriverInstallNative.SERVICE_QUERY_STATUS);

            if (service == nint.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                return DriverInstallResult.Failed("Failed to open driver service", error);
            }

            try
            {
                // Check current state
                if (DriverInstallNative.QueryServiceStatus(service, out var status))
                {
                    if (status.dwCurrentState == DriverInstallNative.SERVICE_RUNNING)
                    {
                        return DriverInstallResult.Succeeded(DriverInstallState.Running);
                    }
                }

                // Start the service
                if (!DriverInstallNative.StartServiceW(service, 0, nint.Zero))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == DriverInstallNative.ERROR_SERVICE_ALREADY_RUNNING)
                    {
                        return DriverInstallResult.Succeeded(DriverInstallState.Running);
                    }

                    return DriverInstallResult.Failed(
                        $"Failed to start driver service: {new Win32Exception(error).Message}",
                        error);
                }

                // Wait for service to start
                for (var i = 0; i < 30; i++)
                {
                    Thread.Sleep(100);
                    if (DriverInstallNative.QueryServiceStatus(service, out status))
                    {
                        if (status.dwCurrentState == DriverInstallNative.SERVICE_RUNNING)
                        {
                            return DriverInstallResult.Succeeded(DriverInstallState.Running);
                        }
                    }
                }

                return DriverInstallResult.Failed("Driver service start timed out");
            }
            finally
            {
                DriverInstallNative.CloseServiceHandle(service);
            }
        }
        finally
        {
            DriverInstallNative.CloseServiceHandle(scManager);
        }
    }

    private DriverInstallResult UninstallDriverInternal()
    {
        lock (_installLock)
        {
            var scManager = DriverInstallNative.OpenSCManagerW(
                null, null, DriverInstallNative.SC_MANAGER_ALL_ACCESS);

            if (scManager == nint.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                return DriverInstallResult.Failed("Failed to open Service Control Manager", error);
            }

            try
            {
                var service = DriverInstallNative.OpenServiceW(
                    scManager, ServiceName,
                    DriverInstallNative.SERVICE_STOP | DriverInstallNative.DELETE |
                    DriverInstallNative.SERVICE_QUERY_STATUS);

                if (service == nint.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == DriverInstallNative.ERROR_SERVICE_DOES_NOT_EXIST)
                    {
                        return DriverInstallResult.Succeeded(DriverInstallState.NotInstalled);
                    }

                    return DriverInstallResult.Failed("Failed to open driver service", error);
                }

                try
                {
                    // Stop the service if running
                    if (DriverInstallNative.QueryServiceStatus(service, out var status))
                    {
                        if (status.dwCurrentState != DriverInstallNative.SERVICE_STOPPED)
                        {
                            DriverInstallNative.ControlService(
                                service,
                                DriverInstallNative.SERVICE_CONTROL_STOP,
                                out _);

                            // Wait for service to stop
                            for (var i = 0; i < 30; i++)
                            {
                                Thread.Sleep(100);
                                if (DriverInstallNative.QueryServiceStatus(service, out status))
                                {
                                    if (status.dwCurrentState == DriverInstallNative.SERVICE_STOPPED)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Delete the service
                    if (!DriverInstallNative.DeleteService(service))
                    {
                        var error = Marshal.GetLastWin32Error();
                        return DriverInstallResult.Failed(
                            $"Failed to delete driver service: {new Win32Exception(error).Message}",
                            error);
                    }

                    return DriverInstallResult.Succeeded(DriverInstallState.NotInstalled);
                }
                finally
                {
                    DriverInstallNative.CloseServiceHandle(service);
                }
            }
            finally
            {
                DriverInstallNative.CloseServiceHandle(scManager);
            }
        }
    }
}
