namespace Lumina.Native.IpHelper;

/// <summary>
/// IP Helper API（Iphlpapi.dll）的 P/Invoke 定义。
/// 使用 LibraryImport 以支持 Native AOT。
/// </summary>
public static partial class IpHelperNative
{
    private const string LibraryName = "Iphlpapi.dll";

    #region Route Table Operations

    /// <summary>
    /// 在 IP 路由表中创建新的路由项。
    /// </summary>
    /// <param name="row">要创建的路由项。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "CreateIpForwardEntry2", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint CreateIpForwardEntry2(in MIB_IPFORWARD_ROW2 row);

    /// <summary>
    /// 从 IP 路由表中删除路由项。
    /// </summary>
    /// <param name="row">要删除的路由项。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "DeleteIpForwardEntry2", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint DeleteIpForwardEntry2(in MIB_IPFORWARD_ROW2 row);

    /// <summary>
    /// 获取 IP 路由表。
    /// </summary>
    /// <param name="family">地址族（AF_INET=2，AF_INET6=23，AF_UNSPEC=0）。</param>
    /// <param name="table">接收路由表指针。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "GetIpForwardTable2", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint GetIpForwardTable2(ushort family, out nint table);

    /// <summary>
    /// 释放由 GetIpForwardTable2 分配的内存。
    /// </summary>
    /// <param name="memory">要释放的内存指针。</param>
    [LibraryImport(LibraryName, EntryPoint = "FreeMibTable", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void FreeMibTable(nint memory);

    /// <summary>
    /// 使用默认值初始化路由表行结构。
    /// </summary>
    /// <param name="row">要初始化的路由项。</param>
    [LibraryImport(LibraryName, EntryPoint = "InitializeIpForwardEntry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void InitializeIpForwardEntry(out MIB_IPFORWARD_ROW2 row);

    #endregion

    #region Interface Operations

    /// <summary>
    /// 获取接口信息。
    /// </summary>
    /// <param name="row">接口行结构（需设置 InterfaceLuid 或 InterfaceIndex）。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "GetIfEntry2", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint GetIfEntry2(ref MIB_IF_ROW2 row);

    /// <summary>
    /// 将接口 LUID 转换为接口索引。
    /// </summary>
    /// <param name="interfaceLuid">接口 LUID。</param>
    /// <param name="interfaceIndex">接收接口索引。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "ConvertInterfaceLuidToIndex", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint ConvertInterfaceLuidToIndex(in ulong interfaceLuid, out uint interfaceIndex);

    /// <summary>
    /// 将接口索引转换为接口 LUID。
    /// </summary>
    /// <param name="interfaceIndex">接口索引。</param>
    /// <param name="interfaceLuid">接收接口 LUID。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "ConvertInterfaceIndexToLuid", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint ConvertInterfaceIndexToLuid(uint interfaceIndex, out ulong interfaceLuid);

    /// <summary>
    /// 将接口 LUID 转换为接口名称。
    /// </summary>
    /// <param name="interfaceLuid">接口 LUID。</param>
    /// <param name="interfaceName">用于接收接口名称的缓冲区。</param>
    /// <param name="length">缓冲区长度。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "ConvertInterfaceLuidToNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint ConvertInterfaceLuidToName(in ulong interfaceLuid, [Out] char[] interfaceName, int length);

    /// <summary>
    /// 将接口别名转换为接口 LUID。
    /// </summary>
    /// <param name="interfaceAlias">接口别名（友好名称）。</param>
    /// <param name="interfaceLuid">接收接口 LUID。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "ConvertInterfaceAliasToLuid", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint ConvertInterfaceAliasToLuid(string interfaceAlias, out ulong interfaceLuid);

    #endregion

    #region IP Address Operations

    /// <summary>
    /// 为接口添加单播 IP 地址。
    /// </summary>
    /// <param name="row">要添加的单播 IP 地址行结构。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "CreateUnicastIpAddressEntry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint CreateUnicastIpAddressEntry(in MIB_UNICASTIPADDRESS_ROW row);

    /// <summary>
    /// 从接口删除单播 IP 地址。
    /// </summary>
    /// <param name="row">要删除的单播 IP 地址行结构。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "DeleteUnicastIpAddressEntry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint DeleteUnicastIpAddressEntry(in MIB_UNICASTIPADDRESS_ROW row);

    /// <summary>
    /// 初始化单播 IP 地址行结构。
    /// </summary>
    /// <param name="row">要初始化的行结构。</param>
    [LibraryImport(LibraryName, EntryPoint = "InitializeUnicastIpAddressEntry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void InitializeUnicastIpAddressEntry(out MIB_UNICASTIPADDRESS_ROW row);

    /// <summary>
    /// 获取所有单播 IP 地址。
    /// </summary>
    /// <param name="family">地址族（AF_INET=2，AF_INET6=23，AF_UNSPEC=0）。</param>
    /// <param name="table">接收地址表指针。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "GetUnicastIpAddressTable", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint GetUnicastIpAddressTable(ushort family, out nint table);

    #endregion

    #region DNS Operations

    /// <summary>
    /// 为接口设置 DNS 配置。
    /// </summary>
    /// <param name="interfaceGuid">接口 GUID。</param>
    /// <param name="settings">要应用的 DNS 设置。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "SetInterfaceDnsSettings", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint SetInterfaceDnsSettings(in Guid interfaceGuid, in DNS_INTERFACE_SETTINGS settings);

    /// <summary>
    /// 获取接口的 DNS 配置。
    /// </summary>
    /// <param name="interfaceGuid">接口 GUID。</param>
    /// <param name="settings">接收 DNS 设置。</param>
    /// <returns>成功返回 NO_ERROR (0)，失败返回错误码。</returns>
    [LibraryImport(LibraryName, EntryPoint = "GetInterfaceDnsSettings", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint GetInterfaceDnsSettings(in Guid interfaceGuid, ref DNS_INTERFACE_SETTINGS settings);

    #endregion

    #region Error Codes

    /// <summary>
    /// 操作成功完成。
    /// </summary>
    public const uint NO_ERROR = 0;

    /// <summary>
    /// 未找到指定元素。
    /// </summary>
    public const uint ERROR_NOT_FOUND = 1168;

    /// <summary>
    /// 访问被拒绝。
    /// </summary>
    public const uint ERROR_ACCESS_DENIED = 5;

    /// <summary>
    /// 参数无效。
    /// </summary>
    public const uint ERROR_INVALID_PARAMETER = 87;

    /// <summary>
    /// 内存不足。
    /// </summary>
    public const uint ERROR_NOT_ENOUGH_MEMORY = 8;

    /// <summary>
    /// 对象已存在。
    /// </summary>
    public const uint ERROR_OBJECT_ALREADY_EXISTS = 5010;

    #endregion
}
