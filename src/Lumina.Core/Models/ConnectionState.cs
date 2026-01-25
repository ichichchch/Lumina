namespace Lumina.Core.Models;

/// <summary>
/// 表示 VPN 隧道的连接状态。
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// 未连接。
    /// </summary>
    Disconnected,

    /// <summary>
    /// 正在建立连接。
    /// </summary>
    Connecting,

    /// <summary>
    /// 已连接且隧道处于活动状态。
    /// </summary>
    Connected,

    /// <summary>
    /// 正在断开连接。
    /// </summary>
    Disconnecting,

    /// <summary>
    /// 发生错误。
    /// </summary>
    Error,
}
