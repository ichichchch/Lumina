namespace Lumina.Core.WireGuard;

/// <summary>
/// 管理 WireGuardNT 驱动生命周期，包括安装与卸载。
/// 驱动文件可通过嵌入资源提供，或在开发场景下通过外部路径提供。
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
    /// 创建新的驱动管理器实例。
    /// </summary>
    /// <param name="driverDirectory">
    /// 存放/查找驱动文件的目录；默认为 %LOCALAPPDATA%\Lumina\Driver。
    /// </param>
    /// <param name="externalDriverPath">
    /// 外部驱动文件路径（可选；用于开发场景而无需嵌入资源）。
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
    /// 获取用于 P/Invoke 加载的 wireguard.dll 的完整路径。
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
                {
                    var dllPath = GetWireGuardDllPath();
                    if (!File.Exists(dllPath))
                    {
                        return DriverInstallResult.Failed($"Driver library not found: {dllPath}");
                    }

                    _ = Lumina.Native.WireGuardNT.WireGuardNtLibraryLoader.TryPreloadAndRegister(dllPath);
                    return DriverInstallResult.Succeeded(DriverInstallState.Running);
                }

            case DriverInstallState.Stopped:
                // 尝试启动服务
                {
                    var startResult = await Task.Run(() => StartDriverService(), cancellationToken);
                    if (!startResult.Success)
                    {
                        return startResult;
                    }

                    var dllPath = GetWireGuardDllPath();
                    if (!File.Exists(dllPath))
                    {
                        return DriverInstallResult.Failed($"Driver library not found: {dllPath}");
                    }

                    _ = Lumina.Native.WireGuardNT.WireGuardNtLibraryLoader.TryPreloadAndRegister(dllPath);
                    return startResult;
                }

            case DriverInstallState.NotInstalled:
                // 安装并启动
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
            // 忽略版本解析错误
        }

        return null;
    }

    /// <summary>
    /// 执行驱动安装的同步内部实现（带互斥锁保护）。
    /// </summary>
    /// <returns>安装结果。</returns>
    private DriverInstallResult InstallDriverInternal()
    {
        lock (_installLock)
        {
            try
            {
                // 确保目录存在
                if (!Directory.Exists(_driverDirectory))
                {
                    Directory.CreateDirectory(_driverDirectory);
                }

                // 提取驱动文件
                ExtractDriverFiles();

                // 注册驱动服务
                return RegisterDriverService();
            }
            catch (Exception ex)
            {
                return DriverInstallResult.Failed($"Installation failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 确保驱动文件（sys/dll）存在：优先从外部路径复制，其次从嵌入资源提取。
    /// </summary>
    private void ExtractDriverFiles()
    {
        var sysTarget = Path.Combine(_driverDirectory, DriverFileName);
        var dllTarget = Path.Combine(_driverDirectory, DllFileName);

        // 若文件已存在则跳过
        var sysExists = File.Exists(sysTarget);
        var dllExists = File.Exists(dllTarget);
        if (sysExists && dllExists)
        {
            return;
        }

        // 优先尝试从外部路径复制（开发场景）
        if (!string.IsNullOrEmpty(_externalDriverPath) && Directory.Exists(_externalDriverPath))
        {
            var externalSys = Path.Combine(_externalDriverPath, DriverFileName);
            var externalDll = Path.Combine(_externalDriverPath, DllFileName);

            if (!sysExists && File.Exists(externalSys))
            {
                File.Copy(externalSys, sysTarget, overwrite: false);
                sysExists = true;
            }

            if (!dllExists && File.Exists(externalDll))
            {
                File.Copy(externalDll, dllTarget, overwrite: false);
                dllExists = true;
            }
        }

        // 尝试从嵌入资源提取
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePrefix = "Lumina.Core.Resources.Driver.";

        var sysExtracted = sysExists || TryExtractResource(assembly, $"{resourcePrefix}{DriverFileName}", sysTarget);
        var dllExtracted = dllExists || TryExtractResource(assembly, $"{resourcePrefix}{DllFileName}", dllTarget);

        if (!sysExtracted || !dllExtracted)
        {
            var missing = new List<string>(capacity: 2);
            if (!sysExtracted)
            {
                missing.Add(DriverFileName);
            }
            if (!dllExtracted)
            {
                missing.Add(DllFileName);
            }

            throw new FileNotFoundException(
                "WireGuardNT driver files not found. " +
                "Either embed wireguard.sys and wireguard.dll as resources in Lumina.Core/Resources/Driver/, " +
                "or provide external driver path via configuration. " +
                $"Missing: {string.Join(", ", missing)}. " +
                $"Expected location: {_driverDirectory}");
        }

        _ = Lumina.Native.WireGuardNT.WireGuardNtLibraryLoader.TryPreloadAndRegister(dllTarget);
    }

    /// <summary>
    /// 尝试从程序集资源中提取指定资源到目标路径。
    /// </summary>
    /// <param name="assembly">包含资源的程序集。</param>
    /// <param name="resourceName">资源名称。</param>
    /// <param name="targetPath">目标文件路径。</param>
    /// <returns>提取成功则返回 true。</returns>
    private static bool TryExtractResource(Assembly assembly, string resourceName, string targetPath)
    {
        if (File.Exists(targetPath))
        {
            return true;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return false;
        }

        using var fileStream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        stream.CopyTo(fileStream);
        return true;
    }

    /// <summary>
    /// 从程序集资源中提取指定资源到目标路径；资源不存在则抛出异常。
    /// </summary>
    /// <param name="assembly">包含资源的程序集。</param>
    /// <param name="resourceName">资源名称。</param>
    /// <param name="targetPath">目标文件路径。</param>
    /// <exception cref="FileNotFoundException">资源不存在时抛出。</exception>
    private static void ExtractResource(Assembly assembly, string resourceName, string targetPath)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            // 开发阶段可接受资源未嵌入；生产环境应确保驱动文件被嵌入或可从目录获取
            throw new FileNotFoundException(
                $"Driver resource '{resourceName}' not found. " +
                "Ensure driver files are embedded as resources or placed in the driver directory.");
        }

        using var fileStream = File.Create(targetPath);
        stream.CopyTo(fileStream);
    }

    /// <summary>
    /// 在系统服务控制管理器中注册内核驱动服务。
    /// </summary>
    /// <returns>注册结果。</returns>
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
            // 检查服务是否已存在
            var existingService = DriverInstallNative.OpenServiceW(
                scManager, ServiceName, DriverInstallNative.SERVICE_ALL_ACCESS);

            if (existingService != nint.Zero)
            {
                DriverInstallNative.CloseServiceHandle(existingService);
                return DriverInstallResult.Succeeded(DriverInstallState.Stopped);
            }

            // 创建服务
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

    /// <summary>
    /// 启动驱动服务并等待其进入运行状态。
    /// </summary>
    /// <returns>启动结果。</returns>
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
                // 检查当前状态
                if (DriverInstallNative.QueryServiceStatus(service, out var status))
                {
                    if (status.dwCurrentState == DriverInstallNative.SERVICE_RUNNING)
                    {
                        return DriverInstallResult.Succeeded(DriverInstallState.Running);
                    }
                }

                // 启动服务
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

                // 等待服务启动
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

    /// <summary>
    /// 执行驱动卸载的同步内部实现（带互斥锁保护）。
    /// </summary>
    /// <returns>卸载结果。</returns>
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
                    // 如果服务正在运行则先停止
                    if (DriverInstallNative.QueryServiceStatus(service, out var status))
                    {
                        if (status.dwCurrentState != DriverInstallNative.SERVICE_STOPPED)
                        {
                            DriverInstallNative.ControlService(
                                service,
                                DriverInstallNative.SERVICE_CONTROL_STOP,
                                out _);

                            // 等待服务停止
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

                    // 删除服务
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
