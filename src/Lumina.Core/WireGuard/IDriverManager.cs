namespace Lumina.Core.WireGuard;

/// <summary>
/// 表示 WireGuard 驱动的安装/运行状态。
/// </summary>
public enum DriverInstallState
{
    /// <summary>驱动未安装。</summary>
    NotInstalled,

    /// <summary>驱动已安装但未运行。</summary>
    Stopped,

    /// <summary>驱动已安装且正在运行。</summary>
    Running,

    /// <summary>驱动正在安装中。</summary>
    Installing,

    /// <summary>驱动安装失败。</summary>
    Error
}

/// <summary>
/// 驱动安装/卸载/就绪检查操作的结果。
/// </summary>
public sealed class DriverInstallResult
{
    /// <summary>
    /// 获取操作是否成功。
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 获取错误消息（若失败）。
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 获取错误码（若失败；具体含义依赖实现）。
    /// </summary>
    public int ErrorCode { get; init; }

    /// <summary>
    /// 获取当前驱动状态。
    /// </summary>
    public DriverInstallState State { get; init; }

    /// <summary>
    /// 创建表示成功的结果对象。
    /// </summary>
    /// <param name="state">成功时的驱动状态。</param>
    /// <returns>成功结果。</returns>
    public static DriverInstallResult Succeeded(DriverInstallState state) => new()
    {
        Success = true,
        State = state
    };

    /// <summary>
    /// 创建表示失败的结果对象。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">错误码。</param>
    /// <returns>失败结果。</returns>
    public static DriverInstallResult Failed(string message, int errorCode = 0) => new()
    {
        Success = false,
        ErrorMessage = message,
        ErrorCode = errorCode,
        State = DriverInstallState.Error
    };
}

/// <summary>
/// 管理 WireGuardNT 驱动生命周期，包括安装、卸载与状态检查。
/// </summary>
public interface IDriverManager
{
    /// <summary>
    /// 获取驱动当前安装/运行状态。
    /// </summary>
    DriverInstallState GetDriverState();

    /// <summary>
    /// 检查驱动是否已安装并可用。
    /// </summary>
    bool IsDriverReady();

    /// <summary>
    /// 从嵌入资源安装 WireGuardNT 驱动。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>安装操作结果。</returns>
    Task<DriverInstallResult> InstallDriverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 卸载 WireGuardNT 驱动。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>卸载操作结果。</returns>
    Task<DriverInstallResult> UninstallDriverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 确保驱动已安装且正在运行；若未安装则尝试安装。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    Task<DriverInstallResult> EnsureDriverReadyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取驱动文件存放路径。
    /// </summary>
    string GetDriverPath();

    /// <summary>
    /// 获取已安装驱动的版本（若可用）。
    /// </summary>
    Version? GetInstalledDriverVersion();
}
