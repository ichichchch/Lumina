namespace Lumina.Native.WireGuardWindows;

public static class EmbeddableTunnelService
{
    private const string ServiceSwitch = "/service";

    public static bool TryRunFromServiceArgs(
        string[] args,
        out int exitCode,
        string? tunnelDllPath = null,
        string? wireGuardDllPath = null)
    {
        exitCode = 0;

        if (args.Length != 2 || !string.Equals(args[0], ServiceSwitch, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var confFile = args[1];
        tunnelDllPath ??= Path.Combine(AppContext.BaseDirectory, "tunnel.dll");
        wireGuardDllPath ??= Path.Combine(AppContext.BaseDirectory, "wireguard.dll");

        _ = WireGuardNtLibraryLoader.TryPreloadAndRegister(wireGuardDllPath);

        if (!File.Exists(tunnelDllPath))
        {
            exitCode = 1;
            return true;
        }

        try
        {
            var ok = RunTunnelService(tunnelDllPath, confFile);
            exitCode = ok ? 0 : 1;
        }
        catch
        {
            exitCode = 1;
        }

        return true;
    }

    private static bool RunTunnelService(string tunnelDllPath, string confFile)
    {
        nint library = nint.Zero;

        try
        {
            library = NativeLibrary.Load(tunnelDllPath);
            var proc = NativeLibrary.GetExport(library, "WireGuardTunnelService");
            unsafe
            {
                var service = (delegate* unmanaged[Cdecl]<char*, int>)proc;
                var confPtr = (char*)Marshal.StringToHGlobalUni(confFile);
                try
                {
                    return service(confPtr) != 0;
                }
                finally
                {
                    Marshal.FreeHGlobal((nint)confPtr);
                }
            }
        }
        finally
        {
            if (library != nint.Zero)
            {
                NativeLibrary.Free(library);
            }
        }
    }
}

