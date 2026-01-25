namespace Lumina.Core.WireGuard;

/// <summary>
/// WireGuard 驱动包装实现。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WireGuardDriver : IWireGuardDriver
{
    private readonly ILogger<WireGuardDriver>? _logger;
    private const string TunnelType = "Lumina";

    /// <summary>
    /// 创建一个新的 WireGuard 驱动包装实例。
    /// </summary>
    /// <param name="logger">可选日志记录器。</param>
    public WireGuardDriver(ILogger<WireGuardDriver>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<WireGuardAdapterHandle> CreateAdapterAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Length > 127)
        {
            throw new ArgumentException("Adapter name must be 127 characters or less", nameof(name));
        }

        _logger?.LogDebug("Creating WireGuard adapter: {AdapterName}", name);

        var handle = WireGuardNative.WireGuardCreateAdapterAutoGuid(name, TunnelType, nint.Zero);

        if (handle.IsInvalid)
        {
            var errorCode = Marshal.GetLastWin32Error();
            _logger?.LogError("Failed to create adapter {AdapterName}: error {ErrorCode}", name, errorCode);
            throw new AdapterCreationException(name, errorCode);
        }

        _logger?.LogInformation("Created WireGuard adapter: {AdapterName}", name);
        return Task.FromResult(handle);
    }

    /// <inheritdoc />
    public Task<WireGuardAdapterHandle?> OpenAdapterAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _logger?.LogDebug("Opening WireGuard adapter: {AdapterName}", name);

        var handle = WireGuardNative.WireGuardOpenAdapter(name);

        if (handle.IsInvalid)
        {
            _logger?.LogDebug("Adapter {AdapterName} not found", name);
            return Task.FromResult<WireGuardAdapterHandle?>(null);
        }

        return Task.FromResult<WireGuardAdapterHandle?>(handle);
    }

    /// <inheritdoc />
    public Task SetConfigurationAsync(WireGuardAdapterHandle handle, TunnelConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(configuration);

        if (handle.IsInvalid)
        {
            throw new ArgumentException("Invalid adapter handle", nameof(handle));
        }

        _logger?.LogDebug("Setting configuration for adapter");

        var configBuffer = BuildConfigurationBuffer(configuration);

        try
        {
            var success = WireGuardNative.WireGuardSetConfiguration(
                handle,
                configBuffer,
                (uint)GetConfigurationSize(configuration));

            if (success == 0)
            {
                var errorCode = Marshal.GetLastWin32Error();
                _logger?.LogError("Failed to set configuration: error {ErrorCode}", errorCode);
                throw new InvalidConfigurationException($"Failed to set WireGuard configuration: error {errorCode}");
            }

            _logger?.LogInformation("Configuration set successfully");
        }
        finally
        {
            Marshal.FreeHGlobal(configBuffer);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<TrafficStats> GetStatsAsync(WireGuardAdapterHandle handle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (handle.IsInvalid)
        {
            throw new ArgumentException("Invalid adapter handle", nameof(handle));
        }

        // 初始缓冲区大小估计
        uint bufferSize = 4096;
        var buffer = Marshal.AllocHGlobal((int)bufferSize);

        try
        {
            var success = WireGuardNative.WireGuardGetConfiguration(handle, buffer, ref bufferSize);

            if (success == 0)
            {
                // 若缓冲区过小则重新分配
                Marshal.FreeHGlobal(buffer);
                buffer = Marshal.AllocHGlobal((int)bufferSize);
                success = WireGuardNative.WireGuardGetConfiguration(handle, buffer, ref bufferSize);
            }

            if (success == 0)
            {
                return Task.FromResult(new TrafficStats());
            }

            return Task.FromResult(ParseTrafficStats(buffer));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <inheritdoc />
    public ulong GetAdapterLuid(WireGuardAdapterHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (handle.IsInvalid)
        {
            throw new ArgumentException("Invalid adapter handle", nameof(handle));
        }

        WireGuardNative.WireGuardGetAdapterLUID(handle, out var luid);
        return luid;
    }

    /// <inheritdoc />
    public Task SetAdapterStateAsync(WireGuardAdapterHandle handle, bool up, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (handle.IsInvalid)
        {
            throw new ArgumentException("Invalid adapter handle", nameof(handle));
        }

        var state = up ? WireGuardAdapterState.Up : WireGuardAdapterState.Down;
        var success = WireGuardNative.WireGuardSetAdapterState(handle, state);

        if (success == 0)
        {
            var errorCode = Marshal.GetLastWin32Error();
            _logger?.LogError("Failed to set adapter state to {State}: error {ErrorCode}", state, errorCode);
            throw new AdapterCreationException($"Failed to set adapter state: error {errorCode}");
        }

        _logger?.LogDebug("Adapter state set to {State}", state);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool IsDriverInstalled()
    {
        try
        {
            var version = WireGuardNative.WireGuardGetRunningDriverVersion();
            return version != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Version? GetDriverVersion()
    {
        try
        {
            var version = WireGuardNative.WireGuardGetRunningDriverVersion();
            if (version == 0)
                return null;

            var major = (int)(version >> 16);
            var minor = (int)(version & 0xFFFF);
            return new Version(major, minor);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 计算将指定隧道配置写入 WireGuard 配置缓冲区所需的字节数。
    /// </summary>
    /// <param name="config">隧道配置。</param>
    /// <returns>所需缓冲区大小（字节）。</returns>
    private static int GetConfigurationSize(TunnelConfiguration config)
    {
        var size = Marshal.SizeOf<WIREGUARD_INTERFACE>();

        foreach (var peer in config.Peers)
        {
            size += Marshal.SizeOf<WIREGUARD_PEER>();
            size += peer.AllowedIPs.Length * Marshal.SizeOf<WIREGUARD_ALLOWED_IP>();
        }

        return size;
    }

    /// <summary>
    /// 构建传递给 WireGuardNT 的配置缓冲区（包含接口、Peer 与 Allowed IP 列表）。
    /// </summary>
    /// <param name="config">隧道配置。</param>
    /// <returns>非托管内存缓冲区指针；调用方需负责释放。</returns>
    private static nint BuildConfigurationBuffer(TunnelConfiguration config)
    {
        var size = GetConfigurationSize(config);
        var buffer = Marshal.AllocHGlobal(size);

        try
        {
            var offset = 0;

            // 构建接口结构
            var iface = new WIREGUARD_INTERFACE
            {
                Flags = WireGuardInterfaceFlags.HasPrivateKey | WireGuardInterfaceFlags.ReplacePeers,
                ListenPort = config.ListenPort,
                PeersCount = (uint)config.Peers.Count,
            };

            // 写入私钥
            if (!string.IsNullOrEmpty(config.PrivateKey))
            {
                var privateKey = Convert.FromBase64String(config.PrivateKey);
                unsafe
                {
                    for (int i = 0; i < 32 && i < privateKey.Length; i++)
                    {
                        iface.PrivateKey[i] = privateKey[i];
                    }
                }
            }

            Marshal.StructureToPtr(iface, buffer + offset, false);
            offset += Marshal.SizeOf<WIREGUARD_INTERFACE>();

            // 构建 Peer 结构
            foreach (var peerConfig in config.Peers)
            {
                var peer = BuildPeerStructure(peerConfig);
                Marshal.StructureToPtr(peer, buffer + offset, false);
                offset += Marshal.SizeOf<WIREGUARD_PEER>();

                // 构建 Allowed IP 列表
                foreach (var allowedIp in peerConfig.AllowedIPs)
                {
                    var allowedIpStruct = BuildAllowedIpStructure(allowedIp);
                    Marshal.StructureToPtr(allowedIpStruct, buffer + offset, false);
                    offset += Marshal.SizeOf<WIREGUARD_ALLOWED_IP>();
                }
            }

            return buffer;
        }
        catch
        {
            Marshal.FreeHGlobal(buffer);
            throw;
        }
    }

    /// <summary>
    /// 将 <see cref="PeerConfiguration"/> 转换为 WireGuardNT 所需的 <see cref="WIREGUARD_PEER"/> 结构。
    /// </summary>
    /// <param name="config">Peer 配置。</param>
    /// <returns>填充后的 <see cref="WIREGUARD_PEER"/>。</returns>
    private static WIREGUARD_PEER BuildPeerStructure(PeerConfiguration config)
    {
        var peer = new WIREGUARD_PEER
        {
            Flags = WireGuardPeerFlags.HasPublicKey | WireGuardPeerFlags.HasEndpoint | WireGuardPeerFlags.ReplaceAllowedIps,
            PersistentKeepalive = config.PersistentKeepalive,
            AllowedIPsCount = (uint)config.AllowedIPs.Length,
        };

        // 写入公钥
        var publicKey = Convert.FromBase64String(config.PublicKey);
        unsafe
        {
            for (int i = 0; i < 32 && i < publicKey.Length; i++)
            {
                peer.PublicKey[i] = publicKey[i];
            }
        }

        // 如存在则写入预共享密钥
        if (!string.IsNullOrEmpty(config.PresharedKey))
        {
            peer.Flags |= WireGuardPeerFlags.HasPresharedKey;
            var psk = Convert.FromBase64String(config.PresharedKey);
            unsafe
            {
                for (int i = 0; i < 32 && i < psk.Length; i++)
                {
                    peer.PresharedKey[i] = psk[i];
                }
            }
        }

        // 设置端点
        if (PeerConfiguration.TryParseEndpoint(config.Endpoint, out var address, out var port) && address is not null)
        {
            peer.Endpoint = CreateEndpoint(address, port);
        }

        if (config.PersistentKeepalive > 0)
        {
            peer.Flags |= WireGuardPeerFlags.HasPersistentKeepalive;
        }

        return peer;
    }

    /// <summary>
    /// 根据 IP 地址与端口构造 WireGuardNT 所需的端点结构。
    /// </summary>
    /// <param name="address">IPv4 或 IPv6 地址。</param>
    /// <param name="port">端口。</param>
    /// <returns>填充后的 <see cref="SOCKADDR_INET"/>。</returns>
    private static SOCKADDR_INET CreateEndpoint(IPAddress address, int port)
    {
        var result = new SOCKADDR_INET();

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            result.Ipv4.Family = AddressFamily.IPv4;
            result.Ipv4.Port = (ushort)IPAddress.HostToNetworkOrder((short)port);
            var bytes = address.GetAddressBytes();
            result.Ipv4.Address = new IN_ADDR(bytes[0], bytes[1], bytes[2], bytes[3]);
        }
        else
        {
            result.Ipv6.Family = AddressFamily.IPv6;
            result.Ipv6.Port = (ushort)IPAddress.HostToNetworkOrder((short)port);
            var bytes = address.GetAddressBytes();
            unsafe
            {
                for (int i = 0; i < 16; i++)
                {
                    result.Ipv6.Address.Bytes[i] = bytes[i];
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 将 CIDR 字符串转换为 WireGuardNT 所需的 <see cref="WIREGUARD_ALLOWED_IP"/> 结构。
    /// </summary>
    /// <param name="cidr">CIDR 字符串。</param>
    /// <returns>填充后的 <see cref="WIREGUARD_ALLOWED_IP"/>。</returns>
    private static WIREGUARD_ALLOWED_IP BuildAllowedIpStructure(string cidr)
    {
        var parts = cidr.Split('/');
        var address = IPAddress.Parse(parts[0]);
        var prefix = byte.Parse(parts[1]);

        var allowedIp = new WIREGUARD_ALLOWED_IP
        {
            Cidr = prefix,
        };

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            allowedIp.AddressFamily = AddressFamily.IPv4;
            var bytes = address.GetAddressBytes();
            allowedIp.Address = new IN_ADDR(bytes[0], bytes[1], bytes[2], bytes[3]);
        }
        else
        {
            allowedIp.AddressFamily = AddressFamily.IPv6;
            var bytes = address.GetAddressBytes();
            unsafe
            {
                for (int i = 0; i < 16; i++)
                {
                    allowedIp.AddressV6.Bytes[i] = bytes[i];
                }
            }
        }

        return allowedIp;
    }

    /// <summary>
    /// 从 WireGuardNT 返回的配置缓冲区中解析流量统计信息。
    /// </summary>
    /// <param name="buffer">配置缓冲区指针。</param>
    /// <returns>解析得到的流量统计。</returns>
    private static TrafficStats ParseTrafficStats(nint buffer)
    {
        var iface = Marshal.PtrToStructure<WIREGUARD_INTERFACE>(buffer);

        ulong totalTx = 0;
        ulong totalRx = 0;
        ulong lastHandshake = 0;

        var offset = Marshal.SizeOf<WIREGUARD_INTERFACE>();

        for (int i = 0; i < iface.PeersCount; i++)
        {
            var peer = Marshal.PtrToStructure<WIREGUARD_PEER>(buffer + offset);
            totalTx += peer.TxBytes;
            totalRx += peer.RxBytes;

            if (peer.LastHandshake > lastHandshake)
            {
                lastHandshake = peer.LastHandshake;
            }

            offset += Marshal.SizeOf<WIREGUARD_PEER>();
            offset += (int)peer.AllowedIPsCount * Marshal.SizeOf<WIREGUARD_ALLOWED_IP>();
        }

        return new TrafficStats
        {
            TxBytes = totalTx,
            RxBytes = totalRx,
            LastHandshakeTime = lastHandshake,
            Timestamp = DateTimeOffset.UtcNow,
        };
    }
}
