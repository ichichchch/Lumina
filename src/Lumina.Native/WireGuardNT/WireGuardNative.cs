namespace Lumina.Native.WireGuardNT;

/// <summary>
/// P/Invoke definitions for WireGuardNT driver (wireguard.dll).
/// Uses LibraryImport for Native AOT compatibility.
/// </summary>
public static partial class WireGuardNative
{
    private const string LibraryName = "wireguard.dll";

    /// <summary>
    /// Creates a new WireGuard adapter.
    /// </summary>
    /// <param name="name">Name of the adapter (max 127 chars).</param>
    /// <param name="tunnelType">Tunnel type identifier.</param>
    /// <param name="requestedGuid">Optional GUID for the adapter.</param>
    /// <returns>Handle to the created adapter, or invalid handle on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardCreateAdapter", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial WireGuardAdapterHandle WireGuardCreateAdapter(
        string name,
        string tunnelType,
        in Guid requestedGuid);

    /// <summary>
    /// Creates a new WireGuard adapter with auto-generated GUID.
    /// </summary>
    /// <param name="name">Name of the adapter (max 127 chars).</param>
    /// <param name="tunnelType">Tunnel type identifier.</param>
    /// <param name="requestedGuid">Pointer to GUID (can be null for auto-generated).</param>
    /// <returns>Handle to the created adapter, or invalid handle on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardCreateAdapter", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial WireGuardAdapterHandle WireGuardCreateAdapterAutoGuid(
        string name,
        string tunnelType,
        nint requestedGuid);

    /// <summary>
    /// Opens an existing WireGuard adapter by name.
    /// </summary>
    /// <param name="name">Name of the adapter to open.</param>
    /// <returns>Handle to the adapter, or invalid handle on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardOpenAdapter", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial WireGuardAdapterHandle WireGuardOpenAdapter(string name);

    /// <summary>
    /// Closes a WireGuard adapter handle.
    /// </summary>
    /// <param name="adapter">Handle to close.</param>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardCloseAdapter", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void WireGuardCloseAdapter(nint adapter);

    /// <summary>
    /// Gets the LUID of the adapter.
    /// </summary>
    /// <param name="adapter">Handle to the adapter.</param>
    /// <param name="luid">Receives the LUID.</param>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardGetAdapterLUID", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void WireGuardGetAdapterLUID(WireGuardAdapterHandle adapter, out ulong luid);

    /// <summary>
    /// Sets the configuration of the adapter.
    /// </summary>
    /// <param name="adapter">Handle to the adapter.</param>
    /// <param name="config">Pointer to the configuration buffer.</param>
    /// <param name="bytes">Size of the configuration buffer.</param>
    /// <returns>Non-zero on success, zero on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardSetConfiguration", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial int WireGuardSetConfiguration(WireGuardAdapterHandle adapter, nint config, uint bytes);

    /// <summary>
    /// Gets the configuration of the adapter.
    /// </summary>
    /// <param name="adapter">Handle to the adapter.</param>
    /// <param name="config">Pointer to receive the configuration buffer.</param>
    /// <param name="bytes">On input, size of the buffer. On output, required size.</param>
    /// <returns>Non-zero on success, zero on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardGetConfiguration", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial int WireGuardGetConfiguration(WireGuardAdapterHandle adapter, nint config, ref uint bytes);

    /// <summary>
    /// Sets the state of the adapter (up or down).
    /// </summary>
    /// <param name="adapter">Handle to the adapter.</param>
    /// <param name="state">Desired state.</param>
    /// <returns>Non-zero on success, zero on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardSetAdapterState", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial int WireGuardSetAdapterState(WireGuardAdapterHandle adapter, WireGuardAdapterState state);

    /// <summary>
    /// Gets the state of the adapter.
    /// </summary>
    /// <param name="adapter">Handle to the adapter.</param>
    /// <param name="state">Receives the current state.</param>
    /// <returns>Non-zero on success, zero on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardGetAdapterState", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial int WireGuardGetAdapterState(WireGuardAdapterHandle adapter, out WireGuardAdapterState state);

    /// <summary>
    /// Sets the logger callback for all adapters.
    /// </summary>
    /// <param name="callback">Pointer to logger callback function, or null to disable logging.</param>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardSetLogger", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void WireGuardSetLogger(nint callback);

    /// <summary>
    /// Gets the running driver version.
    /// </summary>
    /// <returns>Driver version as DWORD (major << 16 | minor), or 0 on error.</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardGetRunningDriverVersion", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint WireGuardGetRunningDriverVersion();
}
