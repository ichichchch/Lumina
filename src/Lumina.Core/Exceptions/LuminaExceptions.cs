namespace Lumina.Core.Exceptions;

/// <summary>
/// Lumina 领域内异常的基类。
/// </summary>
public abstract class LuminaException : Exception
{
    /// <summary>
    /// 初始化 <see cref="LuminaException"/> 的新实例。
    /// </summary>
    protected LuminaException() { }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="LuminaException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    protected LuminaException(string message) : base(message) { }

    /// <summary>
    /// 使用指定的错误消息与内部异常初始化 <see cref="LuminaException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    protected LuminaException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 当 WireGuard 驱动未安装或无法找到时抛出的异常。
/// </summary>
public class DriverNotFoundException : LuminaException
{
    /// <summary>
    /// 初始化 <see cref="DriverNotFoundException"/> 的新实例。
    /// </summary>
    public DriverNotFoundException()
        : base("WireGuard driver (wireguard.dll) not found. Please install WireGuard for Windows.") { }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="DriverNotFoundException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    public DriverNotFoundException(string message) : base(message) { }

    /// <summary>
    /// 使用指定的错误消息与内部异常初始化 <see cref="DriverNotFoundException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    public DriverNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 当创建适配器失败时抛出的异常。
/// </summary>
public class AdapterCreationException : LuminaException
{
    /// <summary>
    /// 获取适配器名称（如果可用）。
    /// </summary>
    public string? AdapterName { get; }

    /// <summary>
    /// 获取与失败相关的错误码。
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// 使用指定的适配器名称与错误码初始化 <see cref="AdapterCreationException"/> 的新实例。
    /// </summary>
    /// <param name="adapterName">适配器名称。</param>
    /// <param name="errorCode">错误码。</param>
    public AdapterCreationException(string adapterName, int errorCode)
        : base($"Failed to create adapter '{adapterName}'. Error code: {errorCode}")
    {
        AdapterName = adapterName;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="AdapterCreationException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    public AdapterCreationException(string message) : base(message) { }

    /// <summary>
    /// 使用指定的错误消息与内部异常初始化 <see cref="AdapterCreationException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    public AdapterCreationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 当路由配置失败时抛出的异常。
/// </summary>
public class RouteConfigurationException : LuminaException
{
    /// <summary>
    /// 获取与失败相关的路由文本（如果可用）。
    /// </summary>
    public string? Route { get; }

    /// <summary>
    /// 获取与失败相关的错误码。
    /// </summary>
    public uint ErrorCode { get; }

    /// <summary>
    /// 使用指定的路由与错误码初始化 <see cref="RouteConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="route">路由文本。</param>
    /// <param name="errorCode">错误码。</param>
    public RouteConfigurationException(string route, uint errorCode)
        : base($"Failed to configure route '{route}'. Error code: {errorCode}")
    {
        Route = route;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="RouteConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    public RouteConfigurationException(string message) : base(message) { }

    /// <summary>
    /// 使用指定的错误消息与内部异常初始化 <see cref="RouteConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    public RouteConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 当 DNS 配置失败时抛出的异常。
/// </summary>
public class DnsConfigurationException : LuminaException
{
    /// <summary>
    /// 获取与失败相关的 DNS 服务器列表（如果可用）。
    /// </summary>
    public string[]? DnsServers { get; }

    /// <summary>
    /// 获取与失败相关的错误码。
    /// </summary>
    public uint ErrorCode { get; }

    /// <summary>
    /// 使用指定的 DNS 服务器列表与错误码初始化 <see cref="DnsConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="dnsServers">DNS 服务器列表。</param>
    /// <param name="errorCode">错误码。</param>
    public DnsConfigurationException(string[] dnsServers, uint errorCode)
        : base($"Failed to configure DNS servers [{string.Join(", ", dnsServers)}]. Error code: {errorCode}")
    {
        DnsServers = dnsServers;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="DnsConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    public DnsConfigurationException(string message) : base(message) { }

    /// <summary>
    /// 使用指定的错误消息与内部异常初始化 <see cref="DnsConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    public DnsConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 当隧道配置无效时抛出的异常。
/// </summary>
public class InvalidConfigurationException : LuminaException
{
    /// <summary>
    /// 获取与失败相关的配置名称（如果可用）。
    /// </summary>
    public string? ConfigurationName { get; }

    /// <summary>
    /// 获取配置校验错误列表。
    /// </summary>
    public string[] ValidationErrors { get; }

    /// <summary>
    /// 使用指定的配置名称与校验错误初始化 <see cref="InvalidConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="configurationName">配置名称。</param>
    /// <param name="validationErrors">校验错误列表。</param>
    public InvalidConfigurationException(string configurationName, string[] validationErrors)
        : base($"Configuration '{configurationName}' is invalid: {string.Join("; ", validationErrors)}")
    {
        ConfigurationName = configurationName;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="InvalidConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    public InvalidConfigurationException(string message) : base(message)
    {
        ValidationErrors = [];
    }

    /// <summary>
    /// 使用指定的错误消息与内部异常初始化 <see cref="InvalidConfigurationException"/> 的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    public InvalidConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
        ValidationErrors = [];
    }
}

/// <summary>
/// 当 VPN 连接相关操作失败时抛出的异常。
/// </summary>
public class ConnectionException : LuminaException
{
    /// <summary>
    /// 获取失败原因。
    /// </summary>
    public ConnectionFailureReason Reason { get; }

    /// <summary>
    /// 使用指定的失败原因与错误消息初始化 <see cref="ConnectionException"/> 的新实例。
    /// </summary>
    /// <param name="reason">失败原因。</param>
    /// <param name="message">错误消息。</param>
    public ConnectionException(ConnectionFailureReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>
    /// 使用指定的失败原因、错误消息与内部异常初始化 <see cref="ConnectionException"/> 的新实例。
    /// </summary>
    /// <param name="reason">失败原因。</param>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    public ConnectionException(ConnectionFailureReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }
}

/// <summary>
/// 连接失败原因枚举。
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
