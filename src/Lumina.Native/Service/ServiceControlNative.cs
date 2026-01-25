namespace Lumina.Native.Service;

public static partial class ServiceControlNative
{
    public const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
    public const uint SC_MANAGER_CONNECT = 0x0001;

    public const uint SERVICE_ALL_ACCESS = 0xF01FF;
    public const uint SERVICE_QUERY_STATUS = 0x0004;
    public const uint SERVICE_START = 0x0010;
    public const uint SERVICE_STOP = 0x0020;
    public const uint DELETE = 0x00010000;

    public const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;

    public const uint SERVICE_AUTO_START = 0x00000002;
    public const uint SERVICE_ERROR_NORMAL = 0x00000001;

    public const uint SERVICE_CONFIG_SID_INFO = 5;
    public const uint SERVICE_SID_TYPE_UNRESTRICTED = 1;

    public const uint SERVICE_CONTROL_STOP = 0x00000001;

    public const int ERROR_SERVICE_ALREADY_RUNNING = 1056;
    public const int ERROR_SERVICE_EXISTS = 1073;
    public const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_SID_INFO
    {
        public uint dwServiceSidType;
    }

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint OpenSCManagerW(
        string? lpMachineName,
        string? lpDatabaseName,
        uint dwDesiredAccess);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseServiceHandle(nint hSCObject);

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateServiceW(
        nint hSCManager,
        string lpServiceName,
        string lpDisplayName,
        uint dwDesiredAccess,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string lpBinaryPathName,
        string? lpLoadOrderGroup,
        nint lpdwTagId,
        nint lpDependencies,
        string? lpServiceStartName,
        string? lpPassword);

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint OpenServiceW(
        nint hSCManager,
        string lpServiceName,
        uint dwDesiredAccess);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool StartServiceW(
        nint hService,
        uint dwNumServiceArgs,
        nint lpServiceArgVectors);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ControlService(
        nint hService,
        uint dwControl,
        out SERVICE_STATUS lpServiceStatus);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteService(nint hService);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool QueryServiceStatus(
        nint hService,
        out SERVICE_STATUS lpServiceStatus);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ChangeServiceConfig2W(
        nint hService,
        uint dwInfoLevel,
        nint lpInfo);
}

