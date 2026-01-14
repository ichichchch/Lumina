namespace Lumina.Core.Exceptions;

/// <summary>
/// Base exception for all Lumina-specific exceptions.
/// </summary>
public abstract class LuminaException : Exception
{
    protected LuminaException() { }
    protected LuminaException(string message) : base(message) { }
    protected LuminaException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when the WireGuard driver is not installed or cannot be found.
/// </summary>
public class DriverNotFoundException : LuminaException
{
    public DriverNotFoundException()
        : base("WireGuard driver (wireguard.dll) not found. Please install WireGuard for Windows.") { }

    public DriverNotFoundException(string message) : base(message) { }
    public DriverNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when adapter creation fails.
/// </summary>
public class AdapterCreationException : LuminaException
{
    public string? AdapterName { get; }
    public int ErrorCode { get; }

    public AdapterCreationException(string adapterName, int errorCode)
        : base($"Failed to create adapter '{adapterName}'. Error code: {errorCode}")
    {
        AdapterName = adapterName;
        ErrorCode = errorCode;
    }

    public AdapterCreationException(string message) : base(message) { }
    public AdapterCreationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when route configuration fails.
/// </summary>
public class RouteConfigurationException : LuminaException
{
    public string? Route { get; }
    public uint ErrorCode { get; }

    public RouteConfigurationException(string route, uint errorCode)
        : base($"Failed to configure route '{route}'. Error code: {errorCode}")
    {
        Route = route;
        ErrorCode = errorCode;
    }

    public RouteConfigurationException(string message) : base(message) { }
    public RouteConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when DNS configuration fails.
/// </summary>
public class DnsConfigurationException : LuminaException
{
    public string[]? DnsServers { get; }
    public uint ErrorCode { get; }

    public DnsConfigurationException(string[] dnsServers, uint errorCode)
        : base($"Failed to configure DNS servers [{string.Join(", ", dnsServers)}]. Error code: {errorCode}")
    {
        DnsServers = dnsServers;
        ErrorCode = errorCode;
    }

    public DnsConfigurationException(string message) : base(message) { }
    public DnsConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when tunnel configuration is invalid.
/// </summary>
public class InvalidConfigurationException : LuminaException
{
    public string? ConfigurationName { get; }
    public string[] ValidationErrors { get; }

    public InvalidConfigurationException(string configurationName, string[] validationErrors)
        : base($"Configuration '{configurationName}' is invalid: {string.Join("; ", validationErrors)}")
    {
        ConfigurationName = configurationName;
        ValidationErrors = validationErrors;
    }

    public InvalidConfigurationException(string message) : base(message)
    {
        ValidationErrors = [];
    }

    public InvalidConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
        ValidationErrors = [];
    }
}

/// <summary>
/// Thrown when a VPN connection operation fails.
/// </summary>
public class ConnectionException : LuminaException
{
    public ConnectionFailureReason Reason { get; }

    public ConnectionException(ConnectionFailureReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    public ConnectionException(ConnectionFailureReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }
}

/// <summary>
/// Reasons for connection failure.
/// </summary>
public enum ConnectionFailureReason
{
    Unknown,
    DriverNotFound,
    AdapterCreationFailed,
    ConfigurationFailed,
    RoutingFailed,
    DnsFailed,
    HandshakeFailed,
    Timeout,
    Cancelled,
}
