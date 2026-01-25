namespace Lumina.Core.Network;

/// <summary>
/// Windows DNS 管理器：通过 netsh 命令设置/恢复接口 DNS。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsDnsManager : IDnsManager, IDisposable
{
    private readonly ILogger<WindowsDnsManager>? _logger;
    private Guid? _modifiedInterfaceGuid;
    private bool _disposed;

    /// <summary>
    /// 创建一个新的 Windows DNS 管理器实例。
    /// </summary>
    /// <param name="logger">可选日志记录器。</param>
    public WindowsDnsManager(ILogger<WindowsDnsManager>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool HasModifiedDns => _modifiedInterfaceGuid.HasValue;

    /// <inheritdoc />
    public async Task SetDnsServersAsync(Guid interfaceGuid, string[] dnsServers, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (dnsServers.Length == 0)
        {
            _logger?.LogDebug("No DNS servers to set");
            return;
        }

        _logger?.LogDebug("Setting DNS servers for interface {InterfaceGuid}: {DnsServers}",
            interfaceGuid, string.Join(", ", dnsServers));

        // Get the interface name from GUID
        var interfaceName = await GetInterfaceNameAsync(interfaceGuid, cancellationToken);

        if (string.IsNullOrEmpty(interfaceName))
        {
            _logger?.LogWarning("Could not find interface name for GUID {InterfaceGuid}", interfaceGuid);
            return;
        }

        // 设置主 DNS
        var primaryDns = dnsServers[0];
        await RunNetshCommandAsync(
            $"interface ipv4 set dnsservers name=\"{interfaceName}\" static {primaryDns} primary validate=no",
            cancellationToken);

        // 追加其它 DNS
        for (int i = 1; i < dnsServers.Length; i++)
        {
            await RunNetshCommandAsync(
                $"interface ipv4 add dnsservers name=\"{interfaceName}\" {dnsServers[i]} index={i + 1} validate=no",
                cancellationToken);
        }

        _modifiedInterfaceGuid = interfaceGuid;

        _logger?.LogInformation("DNS servers set for interface {InterfaceName}: {DnsServers}",
            interfaceName, string.Join(", ", dnsServers));
    }

    /// <inheritdoc />
    public async Task RestoreDnsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_modifiedInterfaceGuid.HasValue)
        {
            _logger?.LogDebug("No DNS settings to restore");
            return;
        }

        var interfaceName = await GetInterfaceNameAsync(_modifiedInterfaceGuid.Value, cancellationToken);

        if (!string.IsNullOrEmpty(interfaceName))
        {
            _logger?.LogDebug("Restoring DNS settings for interface {InterfaceName}", interfaceName);

            // 重置为 DHCP
            await RunNetshCommandAsync(
                $"interface ipv4 set dnsservers name=\"{interfaceName}\" source=dhcp",
                cancellationToken);

            _logger?.LogInformation("DNS settings restored for interface {InterfaceName}", interfaceName);
        }

        _modifiedInterfaceGuid = null;
    }

    /// <summary>
    /// 释放 DNS 管理器并尝试恢复 DNS 设置。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            RestoreDnsAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error restoring DNS during disposal");
        }
    }

    /// <summary>
    /// 根据接口 GUID 获取接口名称（适配器别名）。
    /// </summary>
    /// <param name="interfaceGuid">接口 GUID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>接口名称；如果无法获取则返回 null。</returns>
    private static async Task<string?> GetInterfaceNameAsync(Guid interfaceGuid, CancellationToken cancellationToken)
    {
        // 可通过 netsh 枚举接口并匹配 GUID，或直接使用 IP Helper API 获取别名
        try
        {
            var output = await RunNetshCommandAsync("interface show interface", cancellationToken);

            // 解析输出以定位接口
            // 这里为简化实现：使用系统网络适配器列表按 GUID 匹配
            // 生产环境建议使用 IP Helper API：ConvertInterfaceGuidToLuid + ConvertInterfaceLuidToAlias

            // 回退：从系统 NetworkInterface 列表中获取适配器别名
            var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var adapter = adapters.FirstOrDefault(a =>
            {
                try
                {
                    // 按适配器 ID 比较
                    return a.Id.Equals(interfaceGuid.ToString("B"), StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });

            return adapter?.Name;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 执行 netsh 命令并返回标准输出。
    /// </summary>
    /// <param name="arguments">netsh 参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>标准输出内容。</returns>
    private static async Task<string> RunNetshCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return output;
    }
}
