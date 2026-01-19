namespace Lumina.Native.WireGuardNT;

/// <summary>
/// Safe handle for WireGuard adapter.
/// Ensures proper cleanup when the handle goes out of scope.
/// </summary>
public sealed class WireGuardAdapterHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Creates a new instance with an invalid handle.
    /// </summary>
    public WireGuardAdapterHandle() : base(true)
    {
    }

    /// <summary>
    /// Creates a new instance wrapping an existing handle.
    /// </summary>
    /// <param name="existingHandle">The existing handle to wrap.</param>
    /// <param name="ownsHandle">Whether this instance owns and should release the handle.</param>
    public WireGuardAdapterHandle(nint existingHandle, bool ownsHandle = true) : base(ownsHandle)
    {
        SetHandle(existingHandle);
    }

    /// <summary>
    /// Releases the adapter handle by calling the WireGuard close function.
    /// </summary>
    /// <returns>True if the handle was successfully released.</returns>
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
/// Callback delegate for WireGuard adapter logging.
/// </summary>
/// <param name="level">The log level.</param>
/// <param name="timestamp">The timestamp of the log entry.</param>
/// <param name="message">Pointer to the log message (null-terminated UTF-16 string).</param>
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public unsafe delegate void WireGuardLoggerCallback(
    WireGuardAdapterLogLevel level,
    ulong timestamp,
    char* message);
