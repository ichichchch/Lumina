namespace Lumina.Core.Network;

/// <summary>
/// Windows DNS manager using netsh commands.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsDnsManager : IDnsManager, IDisposable
{
    private readonly ILogger<WindowsDnsManager>? _logger;
    private Guid? _modifiedInterfaceGuid;
    private bool _disposed;

    /// <summary>
    /// Creates a new Windows DNS manager.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
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

        // Set primary DNS server
        var primaryDns = dnsServers[0];
        await RunNetshCommandAsync(
            $"interface ipv4 set dnsservers name=\"{interfaceName}\" static {primaryDns} primary validate=no",
            cancellationToken);

        // Add additional DNS servers
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

            // Reset to DHCP
            await RunNetshCommandAsync(
                $"interface ipv4 set dnsservers name=\"{interfaceName}\" source=dhcp",
                cancellationToken);

            _logger?.LogInformation("DNS settings restored for interface {InterfaceName}", interfaceName);
        }

        _modifiedInterfaceGuid = null;
    }

    /// <summary>
    /// Disposes the DNS manager and restores DNS settings.
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

    private static async Task<string?> GetInterfaceNameAsync(Guid interfaceGuid, CancellationToken cancellationToken)
    {
        // Use netsh to list interfaces and find the one with matching GUID
        // or use IP Helper API to get the alias directly
        try
        {
            var output = await RunNetshCommandAsync("interface show interface", cancellationToken);

            // Parse the output to find the interface
            // For simplicity, we'll try to find it by GUID string
            // In production, use IP Helper API ConvertInterfaceGuidToLuid + ConvertInterfaceLuidToAlias

            // Fallback: use the adapter alias from the system
            var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var adapter = adapters.FirstOrDefault(a =>
            {
                try
                {
                    // Compare based on adapter ID
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
