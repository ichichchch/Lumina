using System.Reflection;
using System.Threading;

namespace Lumina.Native.WireGuardNT;

public static class WireGuardNtLibraryLoader
{
    private const string LibraryName = "wireguard.dll";
    private static int _resolverRegistered;
    private static string? _wireGuardDllPath;
    private static nint _preloadedHandle;

    public static bool TryRegisterResolver(string wireGuardDllPath)
    {
        if (string.IsNullOrWhiteSpace(wireGuardDllPath))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(wireGuardDllPath);
        if (!File.Exists(fullPath))
        {
            return false;
        }

        _wireGuardDllPath ??= fullPath;

        if (Interlocked.Exchange(ref _resolverRegistered, 1) != 0)
        {
            return true;
        }

        NativeLibrary.SetDllImportResolver(typeof(WireGuardNative).Assembly, ResolveWireGuardNt);
        return true;
    }

    public static bool TryPreload(string wireGuardDllPath)
    {
        if (string.IsNullOrWhiteSpace(wireGuardDllPath))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(wireGuardDllPath);
        if (!File.Exists(fullPath))
        {
            return false;
        }

        if (_preloadedHandle != nint.Zero)
        {
            return true;
        }

        if (!NativeLibrary.TryLoad(fullPath, out var handle))
        {
            return false;
        }

        _preloadedHandle = handle;
        return true;
    }

    public static bool TryPreloadAndRegister(string wireGuardDllPath)
    {
        var preloaded = TryPreload(wireGuardDllPath);
        var registered = TryRegisterResolver(wireGuardDllPath);
        return preloaded || registered;
    }

    private static nint ResolveWireGuardNt(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!libraryName.Equals(LibraryName, StringComparison.OrdinalIgnoreCase))
        {
            return nint.Zero;
        }

        var path = _wireGuardDllPath;
        if (path is null || !File.Exists(path))
        {
            return nint.Zero;
        }

        return NativeLibrary.TryLoad(path, out var handle) ? handle : nint.Zero;
    }
}

