namespace Lumina.Native.WireGuardNT;

/// <summary>
/// WireGuardNT 驱动（wireguard.dll）的 P/Invoke 定义。
/// 使用 LibraryImport 以支持 Native AOT。
/// </summary>
public static partial class WireGuardNative
{
    private const string LibraryName = "wireguard.dll";

    /// <summary>
    /// 创建新的 WireGuard 适配器。
    /// </summary>
    /// <param name="name">适配器名称（最大 127 个字符）。</param>
    /// <param name="tunnelType">隧道类型标识。</param>
    /// <param name="requestedGuid">可选的适配器 GUID。</param>
    /// <returns>创建得到的适配器句柄；失败时返回无效句柄。</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardCreateAdapter", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial WireGuardAdapterHandle WireGuardCreateAdapter(
        string name,
        string tunnelType,
        in Guid requestedGuid);

    /// <summary>
    /// 创建新的 WireGuard 适配器（自动生成 GUID）。
    /// </summary>
    /// <param name="name">适配器名称（最大 127 个字符）。</param>
    /// <param name="tunnelType">隧道类型标识。</param>
    /// <param name="requestedGuid">指向 GUID 的指针（可为 null 表示自动生成）。</param>
    /// <returns>创建得到的适配器句柄；失败时返回无效句柄。</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardCreateAdapter", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial WireGuardAdapterHandle WireGuardCreateAdapterAutoGuid(
        string name,
        string tunnelType,
        nint requestedGuid);

    /// <summary>
    /// 按名称打开已存在的 WireGuard 适配器。
    /// </summary>
    /// <param name="name">要打开的适配器名称。</param>
    /// <returns>适配器句柄；失败时返回无效句柄。</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardOpenAdapter", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial WireGuardAdapterHandle WireGuardOpenAdapter(string name);

    /// <summary>
    /// 关闭 WireGuard 适配器句柄。
    /// </summary>
    /// <param name="adapter">要关闭的句柄。</param>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardCloseAdapter", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void WireGuardCloseAdapter(nint adapter);

    /// <summary>
    /// 获取适配器的 LUID。
    /// </summary>
    /// <param name="adapter">适配器句柄。</param>
    /// <param name="luid">接收 LUID。</param>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardGetAdapterLUID", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void WireGuardGetAdapterLUID(WireGuardAdapterHandle adapter, out ulong luid);

    /// <summary>
    /// 设置适配器配置。
    /// </summary>
    /// <param name="adapter">适配器句柄。</param>
    /// <param name="config">配置缓冲区指针。</param>
    /// <param name="bytes">配置缓冲区大小。</param>
    /// <returns>成功返回非 0；失败返回 0。</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardSetConfiguration", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial int WireGuardSetConfiguration(WireGuardAdapterHandle adapter, nint config, uint bytes);

    /// <summary>
    /// 获取适配器配置。
    /// </summary>
    /// <param name="adapter">适配器句柄。</param>
    /// <param name="config">接收配置缓冲区的指针。</param>
    /// <param name="bytes">输入为缓冲区大小；输出为所需大小。</param>
    /// <returns>成功返回非 0；失败返回 0。</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardGetConfiguration", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial int WireGuardGetConfiguration(WireGuardAdapterHandle adapter, nint config, ref uint bytes);

    /// <summary>
    /// 设置适配器状态（up/down）。
    /// </summary>
    /// <param name="adapter">适配器句柄。</param>
    /// <param name="state">目标状态。</param>
    /// <returns>成功返回非 0；失败返回 0。</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardSetAdapterState", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial int WireGuardSetAdapterState(WireGuardAdapterHandle adapter, WireGuardAdapterState state);

    /// <summary>
    /// 获取适配器状态。
    /// </summary>
    /// <param name="adapter">适配器句柄。</param>
    /// <param name="state">接收当前状态。</param>
    /// <returns>成功返回非 0；失败返回 0。</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardGetAdapterState", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial int WireGuardGetAdapterState(WireGuardAdapterHandle adapter, out WireGuardAdapterState state);

    /// <summary>
    /// 为所有适配器设置日志回调。
    /// </summary>
    /// <param name="callback">日志回调函数指针；为 null 则禁用日志。</param>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardSetLogger", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void WireGuardSetLogger(nint callback);

    /// <summary>
    /// 获取正在运行的驱动版本。
    /// </summary>
    /// <returns>以 DWORD 表示的版本（major << 16 | minor）；出错返回 0。</returns>
    [LibraryImport(LibraryName, EntryPoint = "WireGuardGetRunningDriverVersion", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial uint WireGuardGetRunningDriverVersion();
}
