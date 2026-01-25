namespace Lumina.Core.WireGuard;

public static class EmbeddableTunnelServiceEntry
{
    private const string ServiceSwitch = "/service";

    public static bool TryRunFromServiceArgs(string[] args, out int exitCode)
    {
        exitCode = 0;

        if (args.Length != 2 || !string.Equals(args[0], ServiceSwitch, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var baseDirectory = AppContext.BaseDirectory;
        var tunnelDllPath = Path.Combine(baseDirectory, "tunnel.dll");

        var wireGuardDllPath = Path.Combine(baseDirectory, "wireguard.dll");
        if (!File.Exists(wireGuardDllPath))
        {
            var driverManager = new WireGuardDriverManager();
            _ = driverManager.EnsureDriverReadyAsync().GetAwaiter().GetResult();
            wireGuardDllPath = driverManager.GetWireGuardDllPath();
        }

        return Lumina.Native.WireGuardWindows.EmbeddableTunnelService.TryRunFromServiceArgs(
            args,
            out exitCode,
            tunnelDllPath,
            wireGuardDllPath);
    }
}

