using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Lumina.Native.Common;

/// <summary>
/// Utility methods for Win32 error handling.
/// </summary>
public static class Win32Error
{
    /// <summary>
    /// Throws a Win32Exception if the error code indicates failure.
    /// </summary>
    /// <param name="errorCode">The Win32 error code.</param>
    /// <param name="operation">Description of the operation that failed.</param>
    /// <exception cref="Win32Exception">Thrown when errorCode is non-zero.</exception>
    public static void ThrowIfFailed(uint errorCode, string operation)
    {
        if (errorCode != 0)
        {
            throw new Win32Exception((int)errorCode, $"{operation} failed with error code {errorCode}");
        }
    }

    /// <summary>
    /// Throws a Win32Exception for the last Win32 error if condition is true.
    /// </summary>
    /// <param name="condition">If true, throws an exception.</param>
    /// <param name="operation">Description of the operation that failed.</param>
    /// <exception cref="Win32Exception">Thrown when condition is true.</exception>
    public static void ThrowLastErrorIf(bool condition, string operation)
    {
        if (condition)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode, $"{operation} failed with error code {errorCode}");
        }
    }

    /// <summary>
    /// Gets the message for a Win32 error code.
    /// </summary>
    /// <param name="errorCode">The Win32 error code.</param>
    /// <returns>The error message.</returns>
    public static string GetMessage(uint errorCode)
    {
        return new Win32Exception((int)errorCode).Message;
    }
}
