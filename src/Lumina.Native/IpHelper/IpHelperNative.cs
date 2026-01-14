using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lumina.Native.IpHelper;

/// <summary>
/// P/Invoke definitions for IP Helper API (Iphlpapi.dll).
/// Uses LibraryImport for Native AOT compatibility.
/// </summary>
public static partial class IpHelperNative
{
    private const string LibraryName = "Iphlpapi.dll";

    #region Route Table Operations

    /// <summary>
    /// Creates a new route entry in the IP routing table.
    /// </summary>
    /// <param name="row">Route entry to create.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "CreateIpForwardEntry2", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint CreateIpForwardEntry2(in MIB_IPFORWARD_ROW2 row);

    /// <summary>
    /// Deletes a route entry from the IP routing table.
    /// </summary>
    /// <param name="row">Route entry to delete.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "DeleteIpForwardEntry2", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint DeleteIpForwardEntry2(in MIB_IPFORWARD_ROW2 row);

    /// <summary>
    /// Gets the IP routing table.
    /// </summary>
    /// <param name="family">Address family (AF_INET=2, AF_INET6=23, AF_UNSPEC=0).</param>
    /// <param name="table">Receives pointer to the routing table.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "GetIpForwardTable2", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint GetIpForwardTable2(ushort family, out nint table);

    /// <summary>
    /// Frees memory allocated by GetIpForwardTable2.
    /// </summary>
    /// <param name="memory">Pointer to the memory to free.</param>
    [LibraryImport(LibraryName, EntryPoint = "FreeMibTable", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void FreeMibTable(nint memory);

    /// <summary>
    /// Initializes a route table row with default values.
    /// </summary>
    /// <param name="row">Route entry to initialize.</param>
    [LibraryImport(LibraryName, EntryPoint = "InitializeIpForwardEntry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void InitializeIpForwardEntry(out MIB_IPFORWARD_ROW2 row);

    #endregion

    #region Interface Operations

    /// <summary>
    /// Gets interface information by index.
    /// </summary>
    /// <param name="row">Interface row (InterfaceLuid or InterfaceIndex must be set).</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "GetIfEntry2", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint GetIfEntry2(ref MIB_IF_ROW2 row);

    /// <summary>
    /// Converts interface LUID to index.
    /// </summary>
    /// <param name="interfaceLuid">Interface LUID.</param>
    /// <param name="interfaceIndex">Receives interface index.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "ConvertInterfaceLuidToIndex", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint ConvertInterfaceLuidToIndex(in ulong interfaceLuid, out uint interfaceIndex);

    /// <summary>
    /// Converts interface index to LUID.
    /// </summary>
    /// <param name="interfaceIndex">Interface index.</param>
    /// <param name="interfaceLuid">Receives interface LUID.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "ConvertInterfaceIndexToLuid", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint ConvertInterfaceIndexToLuid(uint interfaceIndex, out ulong interfaceLuid);

    /// <summary>
    /// Converts interface LUID to name.
    /// </summary>
    /// <param name="interfaceLuid">Interface LUID.</param>
    /// <param name="interfaceName">Buffer to receive interface name.</param>
    /// <param name="length">Length of the buffer.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "ConvertInterfaceLuidToNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint ConvertInterfaceLuidToName(in ulong interfaceLuid, [Out] char[] interfaceName, int length);

    /// <summary>
    /// Converts interface alias to LUID.
    /// </summary>
    /// <param name="interfaceAlias">Interface alias (friendly name).</param>
    /// <param name="interfaceLuid">Receives interface LUID.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "ConvertInterfaceAliasToLuid", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint ConvertInterfaceAliasToLuid(string interfaceAlias, out ulong interfaceLuid);

    #endregion

    #region IP Address Operations

    /// <summary>
    /// Adds a unicast IP address to an interface.
    /// </summary>
    /// <param name="row">Unicast IP address row to add.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "CreateUnicastIpAddressEntry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint CreateUnicastIpAddressEntry(in MIB_UNICASTIPADDRESS_ROW row);

    /// <summary>
    /// Deletes a unicast IP address from an interface.
    /// </summary>
    /// <param name="row">Unicast IP address row to delete.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "DeleteUnicastIpAddressEntry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint DeleteUnicastIpAddressEntry(in MIB_UNICASTIPADDRESS_ROW row);

    /// <summary>
    /// Initializes a unicast IP address row.
    /// </summary>
    /// <param name="row">Row to initialize.</param>
    [LibraryImport(LibraryName, EntryPoint = "InitializeUnicastIpAddressEntry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void InitializeUnicastIpAddressEntry(out MIB_UNICASTIPADDRESS_ROW row);

    /// <summary>
    /// Gets all unicast IP addresses.
    /// </summary>
    /// <param name="family">Address family (AF_INET=2, AF_INET6=23, AF_UNSPEC=0).</param>
    /// <param name="table">Receives pointer to the address table.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "GetUnicastIpAddressTable", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint GetUnicastIpAddressTable(ushort family, out nint table);

    #endregion

    #region DNS Operations

    /// <summary>
    /// Sets DNS settings for an interface.
    /// </summary>
    /// <param name="interfaceGuid">GUID of the interface.</param>
    /// <param name="settings">DNS settings to apply.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "SetInterfaceDnsSettings", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint SetInterfaceDnsSettings(in Guid interfaceGuid, in DNS_INTERFACE_SETTINGS settings);

    /// <summary>
    /// Gets DNS settings for an interface.
    /// </summary>
    /// <param name="interfaceGuid">GUID of the interface.</param>
    /// <param name="settings">Receives DNS settings.</param>
    /// <returns>NO_ERROR (0) on success, error code on failure.</returns>
    [LibraryImport(LibraryName, EntryPoint = "GetInterfaceDnsSettings", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint GetInterfaceDnsSettings(in Guid interfaceGuid, ref DNS_INTERFACE_SETTINGS settings);

    #endregion

    #region Error Codes

    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    public const uint NO_ERROR = 0;

    /// <summary>
    /// Element not found.
    /// </summary>
    public const uint ERROR_NOT_FOUND = 1168;

    /// <summary>
    /// Access denied.
    /// </summary>
    public const uint ERROR_ACCESS_DENIED = 5;

    /// <summary>
    /// Invalid parameter.
    /// </summary>
    public const uint ERROR_INVALID_PARAMETER = 87;

    /// <summary>
    /// Not enough memory.
    /// </summary>
    public const uint ERROR_NOT_ENOUGH_MEMORY = 8;

    /// <summary>
    /// Object already exists.
    /// </summary>
    public const uint ERROR_OBJECT_ALREADY_EXISTS = 5010;

    #endregion
}
