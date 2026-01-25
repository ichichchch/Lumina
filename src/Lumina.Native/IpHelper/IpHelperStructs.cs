namespace Lumina.Native.IpHelper;

/// <summary>
/// 路由协议类型。
/// </summary>
public enum NL_ROUTE_PROTOCOL : uint
{
    Other = 1,
    Local = 2,
    NetMgmt = 3,
    Icmp = 4,
    Egp = 5,
    Ggp = 6,
    Hello = 7,
    Rip = 8,
    IsIs = 9,
    EsIs = 10,
    Cisco = 11,
    Bbn = 12,
    Ospf = 13,
    Bgp = 14,
    Idpr = 15,
    Eigrp = 16,
    Dvmrp = 17,
    Rpl = 18,
    Dhcp = 19,
    Nt_Autostatic = 10002,
    Nt_Static = 10006,
    Nt_Static_Non_Dod = 10007,
}

/// <summary>
/// 路由来源类型。
/// </summary>
public enum NL_ROUTE_ORIGIN : uint
{
    Manual = 0,
    WellKnown = 1,
    Dhcp = 2,
    RouterAdvertisement = 3,
    _6to4 = 4,
}

/// <summary>
/// 网络前缀来源。
/// </summary>
public enum NL_PREFIX_ORIGIN : uint
{
    Other = 0,
    Manual = 1,
    WellKnown = 2,
    Dhcp = 3,
    RouterAdvertisement = 4,
}

/// <summary>
/// 网络后缀来源。
/// </summary>
public enum NL_SUFFIX_ORIGIN : uint
{
    Other = 0,
    Manual = 1,
    WellKnown = 2,
    OriginDhcp = 3,
    LinkLayerAddress = 4,
    Random = 5,
}

/// <summary>
/// IP 地址的 DAD（重复地址检测）状态。
/// </summary>
public enum NL_DAD_STATE : uint
{
    Invalid = 0,
    Tentative = 1,
    Duplicate = 2,
    Deprecated = 3,
    Preferred = 4,
}

/// <summary>
/// IPv4/IPv6 路由表项结构（MIB_IPFORWARD_ROW2）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MIB_IPFORWARD_ROW2
{
    public ulong InterfaceLuid;
    public uint InterfaceIndex;
    public IP_ADDRESS_PREFIX DestinationPrefix;
    public SOCKADDR_INET NextHop;
    public byte SitePrefixLength;
    public uint ValidLifetime;
    public uint PreferredLifetime;
    public uint Metric;
    public NL_ROUTE_PROTOCOL Protocol;
    [MarshalAs(UnmanagedType.U1)]
    public bool Loopback;
    [MarshalAs(UnmanagedType.U1)]
    public bool AutoconfigureAddress;
    [MarshalAs(UnmanagedType.U1)]
    public bool Publish;
    [MarshalAs(UnmanagedType.U1)]
    public bool Immortal;
    public uint Age;
    public NL_ROUTE_ORIGIN Origin;
}

/// <summary>
/// IP 地址前缀结构。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct IP_ADDRESS_PREFIX
{
    public SOCKADDR_INET Prefix;
    public byte PrefixLength;
}

/// <summary>
/// 单播 IP 地址行结构（MIB_UNICASTIPADDRESS_ROW）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MIB_UNICASTIPADDRESS_ROW
{
    public SOCKADDR_INET Address;
    public ulong InterfaceLuid;
    public uint InterfaceIndex;
    public NL_PREFIX_ORIGIN PrefixOrigin;
    public NL_SUFFIX_ORIGIN SuffixOrigin;
    public uint ValidLifetime;
    public uint PreferredLifetime;
    public byte OnLinkPrefixLength;
    [MarshalAs(UnmanagedType.U1)]
    public bool SkipAsSource;
    public NL_DAD_STATE DadState;
    public uint ScopeId;
    public long CreationTimeStamp;
}

/// <summary>
/// 网络接口运行状态。
/// </summary>
public enum IF_OPER_STATUS : uint
{
    Up = 1,
    Down = 2,
    Testing = 3,
    Unknown = 4,
    Dormant = 5,
    NotPresent = 6,
    LowerLayerDown = 7,
}

/// <summary>
/// 网络接口类型。
/// </summary>
public enum IF_TYPE : uint
{
    Other = 1,
    EthernetCSMACD = 6,
    Iso88025TokenRing = 9,
    PPP = 23,
    SoftwareLoopback = 24,
    ATM = 37,
    IEEE80211 = 71,
    Tunnel = 131,
    IEEE1394 = 144,
}

/// <summary>
/// 网络接口行结构（MIB_IF_ROW2）。
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct MIB_IF_ROW2
{
    public ulong InterfaceLuid;
    public uint InterfaceIndex;
    public Guid InterfaceGuid;
    public fixed char Alias[256 + 1];
    public fixed char Description[256 + 1];
    public uint PhysicalAddressLength;
    public fixed byte PhysicalAddress[32];
    public fixed byte PermanentPhysicalAddress[32];
    public uint Mtu;
    public IF_TYPE Type;
    public uint TunnelType;
    public uint MediaType;
    public uint PhysicalMediumType;
    public uint AccessType;
    public uint DirectionType;
    public byte InterfaceAndOperStatusFlags;
    public IF_OPER_STATUS OperStatus;
    public uint AdminStatus;
    public uint MediaConnectState;
    public Guid NetworkGuid;
    public uint ConnectionType;
    public ulong TransmitLinkSpeed;
    public ulong ReceiveLinkSpeed;
    public ulong InOctets;
    public ulong InUcastPkts;
    public ulong InNUcastPkts;
    public ulong InDiscards;
    public ulong InErrors;
    public ulong InUnknownProtos;
    public ulong InUcastOctets;
    public ulong InMulticastOctets;
    public ulong InBroadcastOctets;
    public ulong OutOctets;
    public ulong OutUcastPkts;
    public ulong OutNUcastPkts;
    public ulong OutDiscards;
    public ulong OutErrors;
    public ulong OutUcastOctets;
    public ulong OutMulticastOctets;
    public ulong OutBroadcastOctets;
    public ulong OutQLen;
}

/// <summary>
/// IP 路由表头结构（MIB_IPFORWARD_TABLE2）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MIB_IPFORWARD_TABLE2
{
    public uint NumEntries;
    // 后续紧跟 MIB_IPFORWARD_ROW2 数组
}

/// <summary>
/// 单播 IP 地址表头结构（MIB_UNICASTIPADDRESS_TABLE）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MIB_UNICASTIPADDRESS_TABLE
{
    public uint NumEntries;
    // 后续紧跟 MIB_UNICASTIPADDRESS_ROW 数组
}

/// <summary>
/// 接口 DNS 设置结构。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DNS_INTERFACE_SETTINGS
{
    public uint Version;
    public ulong Flags;
    public nint Domain;
    public nint NameServer;
    public nint SearchList;
    public uint RegistrationEnabled;
    public uint RegisterAdapterName;
    public uint EnableLLMNR;
    public uint QueryAdapterName;
    public nint ProfileNameServer;
}

/// <summary>
/// DNS 接口设置标志位。
/// </summary>
[Flags]
public enum DNS_SETTING_FLAGS : ulong
{
    None = 0,
    NameServer = 0x1,
    SearchList = 0x2,
    RegistrationEnabled = 0x4,
    RegisterAdapterName = 0x8,
    Domain = 0x10,
    Hostname = 0x20,
    EnableLLMNR = 0x80,
    QueryAdapterName = 0x100,
    ProfileNameServer = 0x200,
    DisableUnconstrainedQueries = 0x400,
    SupplementalSearchList = 0x800,
}
