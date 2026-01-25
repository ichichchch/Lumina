using System.Linq;
using Lumina.Native.Service;

namespace Lumina.Native.WireGuardWindows;

public static class WireGuardTunnelWindowsServiceInstaller
{
    public static bool TryInstall(
        string tunnelName,
        string displayName,
        string executablePath,
        string configFilePath,
        out int win32Error)
    {
        win32Error = 0;

        if (string.IsNullOrWhiteSpace(tunnelName) ||
            string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(executablePath) ||
            string.IsNullOrWhiteSpace(configFilePath))
        {
            win32Error = 87;
            return false;
        }

        var serviceName = GetServiceName(tunnelName);
        var binaryPath = BuildBinaryPath(executablePath, configFilePath);

        var scManager = ServiceControlNative.OpenSCManagerW(null, null, ServiceControlNative.SC_MANAGER_ALL_ACCESS);
        if (scManager == nint.Zero)
        {
            win32Error = Marshal.GetLastWin32Error();
            return false;
        }

        try
        {
            var dependencies = new[] { "Nsi", "TcpIp" };
            var multiSz = BuildMultiSz(dependencies);

            unsafe
            {
                fixed (char* depsPtr = multiSz)
                {
                    var service = ServiceControlNative.CreateServiceW(
                        scManager,
                        serviceName,
                        displayName,
                        ServiceControlNative.SERVICE_ALL_ACCESS,
                        ServiceControlNative.SERVICE_WIN32_OWN_PROCESS,
                        ServiceControlNative.SERVICE_AUTO_START,
                        ServiceControlNative.SERVICE_ERROR_NORMAL,
                        binaryPath,
                        null,
                        nint.Zero,
                        (nint)depsPtr,
                        null,
                        null);

                    if (service == nint.Zero)
                    {
                        win32Error = Marshal.GetLastWin32Error();
                        return win32Error == ServiceControlNative.ERROR_SERVICE_EXISTS;
                    }

                    try
                    {
                        var sidInfo = new ServiceControlNative.SERVICE_SID_INFO
                        {
                            dwServiceSidType = ServiceControlNative.SERVICE_SID_TYPE_UNRESTRICTED
                        };

                        var size = Marshal.SizeOf<ServiceControlNative.SERVICE_SID_INFO>();
                        var buffer = Marshal.AllocHGlobal(size);
                        try
                        {
                            Marshal.StructureToPtr(sidInfo, buffer, fDeleteOld: false);
                            if (!ServiceControlNative.ChangeServiceConfig2W(
                                service,
                                ServiceControlNative.SERVICE_CONFIG_SID_INFO,
                                buffer))
                            {
                                win32Error = Marshal.GetLastWin32Error();
                                return false;
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(buffer);
                        }

                        return true;
                    }
                    finally
                    {
                        ServiceControlNative.CloseServiceHandle(service);
                    }
                }
            }
        }
        finally
        {
            ServiceControlNative.CloseServiceHandle(scManager);
        }
    }

    public static bool TryUninstall(string tunnelName, out int win32Error)
    {
        win32Error = 0;

        if (string.IsNullOrWhiteSpace(tunnelName))
        {
            win32Error = 87;
            return false;
        }

        var serviceName = GetServiceName(tunnelName);
        var scManager = ServiceControlNative.OpenSCManagerW(null, null, ServiceControlNative.SC_MANAGER_ALL_ACCESS);
        if (scManager == nint.Zero)
        {
            win32Error = Marshal.GetLastWin32Error();
            return false;
        }

        try
        {
            var service = ServiceControlNative.OpenServiceW(
                scManager,
                serviceName,
                ServiceControlNative.SERVICE_STOP | ServiceControlNative.DELETE | ServiceControlNative.SERVICE_QUERY_STATUS);

            if (service == nint.Zero)
            {
                win32Error = Marshal.GetLastWin32Error();
                return win32Error == ServiceControlNative.ERROR_SERVICE_DOES_NOT_EXIST;
            }

            try
            {
                _ = ServiceControlNative.ControlService(service, ServiceControlNative.SERVICE_CONTROL_STOP, out _);

                if (!ServiceControlNative.DeleteService(service))
                {
                    win32Error = Marshal.GetLastWin32Error();
                    return false;
                }

                return true;
            }
            finally
            {
                ServiceControlNative.CloseServiceHandle(service);
            }
        }
        finally
        {
            ServiceControlNative.CloseServiceHandle(scManager);
        }
    }

    public static string GetServiceName(string tunnelName)
    {
        return $"WireGuardTunnel${tunnelName}";
    }

    private static string BuildBinaryPath(string executablePath, string configFilePath)
    {
        var exe = Path.GetFullPath(executablePath);
        var conf = Path.GetFullPath(configFilePath);
        return $"{Quote(exe)} /service {Quote(conf)}";
    }

    private static string Quote(string value)
    {
        return $"\"{value}\"";
    }

    private static char[] BuildMultiSz(string[] values)
    {
        if (values.Length == 0)
        {
            return ['\0', '\0'];
        }

        var totalChars = values.Sum(v => v.Length + 1) + 1;
        var buffer = new char[totalChars];
        var offset = 0;

        foreach (var value in values)
        {
            value.CopyTo(0, buffer, offset, value.Length);
            offset += value.Length;
            buffer[offset++] = '\0';
        }

        buffer[offset] = '\0';
        return buffer;
    }
}

