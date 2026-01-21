namespace Lumina.Native.WireGuardNT;

/// <summary>
/// Safe handle for WireGuard adapter.
/// Ensures proper cleanup when the handle goes out of scope.
/// 安全句柄：用于 WireGuard 适配器。
/// 当句柄超出作用域时确保正确清理资源。
/// </summary>
public sealed class WireGuardAdapterHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Creates a new instance with an invalid handle.
    /// 创建一个具有无效句柄的新实例。
    /// </summary>
    public WireGuardAdapterHandle() : base(true)
    {
    }

    /// <summary>
    /// Creates a new instance wrapping an existing handle.
    /// </summary>
    /// <param name="existingHandle">The existing handle to wrap.</param>
    /// <param name="ownsHandle">Whether this instance owns and should release the handle.</param>
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
    /// Releases the adapter handle by calling the WireGuard close function.
    /// </summary>
    /// <returns>True if the handle was successfully released.</returns>
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

///// <summary>
///// Callback delegate for WireGuard adapter logging.
///// </summary>
///// <param name="level">The log level.</param>
///// <param name="timestamp">The timestamp of the log entry.</param>
///// <param name="message">Pointer to the log message (null-terminated UTF-16 string).</param>
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public unsafe delegate void WireGuardLoggerCallback(
    WireGuardAdapterLogLevel level,
    ulong timestamp,
    char* message);

/// <summary>
/// Callback delegate for WireGuard adapter logging.
/// WireGuard 适配器日志回调委托。
/// </summary>
/// <param name="level">The log level. 日志级别。</param>
/// <param name="timestamp">The timestamp of the log entry. 日志条目的时间戳（以毫微秒或实现定义为准）。</param>
/// <param name="message">Pointer to the log message (null-terminated UTF-16 string). 指向日志消息的指针（以 null 结尾的 UTF-16 字符串）。</param>
