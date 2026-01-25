namespace Lumina.Native.WireGuardNT;

/// <summary>
/// WireGuard 适配器安全句柄。
/// 当句柄超出作用域时确保正确清理资源。
/// </summary>
public sealed class WireGuardAdapterHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// 创建一个新的实例（初始为无效句柄）。
    /// </summary>
    public WireGuardAdapterHandle() : base(true)
    {
    }

    /// <summary>
    /// 创建一个新的实例以封装现有句柄。
    /// </summary>
    /// <param name="existingHandle">要封装的现有句柄。</param>
    /// <param name="ownsHandle">指示此实例是否拥有并应释放该句柄。</param>
    public WireGuardAdapterHandle(nint existingHandle, bool ownsHandle = true) : base(ownsHandle)
    {
        SetHandle(existingHandle);
    }

    /// <summary>
    /// 通过调用 WireGuard 的关闭函数释放适配器句柄。
    /// </summary>
    /// <returns>如果句柄成功释放则返回 true。</returns>
    protected override bool ReleaseHandle()
    {
        if (handle != nint.Zero)
        {
            WireGuardNative.WireGuardCloseAdapter(handle);
        }
        return true;
    }
}

/// <summary>
/// WireGuard 适配器日志回调委托。
/// </summary>
/// <param name="level">日志级别。</param>
/// <param name="timestamp">日志条目的时间戳（单位与含义由驱动实现决定）。</param>
/// <param name="message">指向日志消息的指针（以 null 结尾的 UTF-16 字符串）。</param>
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public unsafe delegate void WireGuardLoggerCallback(
    WireGuardAdapterLogLevel level,
    ulong timestamp,
    char* message);
