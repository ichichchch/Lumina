namespace Lumina.Native.Common;

/// <summary>
/// Win32 错误处理相关的工具方法。
/// </summary>
public static class Win32Error
{
    /// <summary>
    /// 当错误码表示失败时抛出 <see cref="Win32Exception"/>。
    /// </summary>
    /// <param name="errorCode">Win32 错误码。</param>
    /// <param name="operation">失败操作的描述。</param>
    /// <exception cref="Win32Exception">当 <paramref name="errorCode"/> 非 0 时抛出。</exception>
    public static void ThrowIfFailed(uint errorCode, string operation)
    {
        if (errorCode != 0)
        {
            throw new Win32Exception((int)errorCode, $"{operation} failed with error code {errorCode}");
        }
    }

    /// <summary>
    /// 当条件为 true 时，使用最后一次 Win32 错误码抛出 <see cref="Win32Exception"/>。
    /// </summary>
    /// <param name="condition">为 true 时抛出异常。</param>
    /// <param name="operation">失败操作的描述。</param>
    /// <exception cref="Win32Exception">当 <paramref name="condition"/> 为 true 时抛出。</exception>
    public static void ThrowLastErrorIf(bool condition, string operation)
    {
        if (condition)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode, $"{operation} failed with error code {errorCode}");
        }
    }

    /// <summary>
    /// 获取指定 Win32 错误码对应的错误消息。
    /// </summary>
    /// <param name="errorCode">Win32 错误码。</param>
    /// <returns>错误消息。</returns>
    public static string GetMessage(uint errorCode)
    {
        return new Win32Exception((int)errorCode).Message;
    }
}
