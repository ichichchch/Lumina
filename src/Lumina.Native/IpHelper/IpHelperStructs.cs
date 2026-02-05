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
/// 在 64 位 Windows 上大小为 104 字节。
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 104)]
public struct MIB_IPFORWARD_ROW2
{
    [FieldOffset(0)]
    public ulong InterfaceLuid;           // 8 bytes (0-7)

    [FieldOffset(8)]
    public uint InterfaceIndex;           // 4 bytes (8-11)

    [FieldOffset(12)]
    public IP_ADDRESS_PREFIX DestinationPrefix; // 32 bytes (12-43)

    [FieldOffset(44)]
    public SOCKADDR_INET NextHop;         // 28 bytes (44-71)

    [FieldOffset(72)]
    public byte SitePrefixLength;         // 1 byte

    // 3 bytes padding (73-75)

    [FieldOffset(76)]
    public uint ValidLifetime;            // 4 bytes (76-79)

    [FieldOffset(80)]
    public uint PreferredLifetime;        // 4 bytes (80-83)

    [FieldOffset(84)]
    public uint Metric;                   // 4 bytes (84-87)

    [FieldOffset(88)]
    public NL_ROUTE_PROTOCOL Protocol;    // 4 bytes (88-91)

    [FieldOffset(92)]
    [MarshalAs(UnmanagedType.U1)]
    public bool Loopback;                 // 1 byte

    [FieldOffset(93)]
    [MarshalAs(UnmanagedType.U1)]
    public bool AutoconfigureAddress;     // 1 byte

    [FieldOffset(94)]
    [MarshalAs(UnmanagedType.U1)]
    public bool Publish;                  // 1 byte

    [FieldOffset(95)]
    [MarshalAs(UnmanagedType.U1)]
    public bool Immortal;                 // 1 byte

    [FieldOffset(96)]
    public uint Age;                      // 4 bytes (96-99)

    [FieldOffset(100)]
    public NL_ROUTE_ORIGIN Origin;        // 4 bytes (100-103)
}

/// <summary>
/// IP 地址前缀结构。
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct IP_ADDRESS_PREFIX
{
    [FieldOffset(0)]
    public SOCKADDR_INET Prefix;          // 28 bytes (0-27)

    [FieldOffset(28)]
    public byte PrefixLength;             // 1 byte

    // 3 bytes padding (29-31)
}

/// <summary>
/// 单播 IP 地址行结构（MIB_UNICASTIPADDRESS_ROW）。
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 80)]
public struct MIB_UNICASTIPADDRESS_ROW
{
    [FieldOffset(0)]
    public SOCKADDR_INET Address;         // 28 bytes

    // 4 bytes padding (28-31) for 8-byte alignment of InterfaceLuid

    [FieldOffset(32)]
    public ulong InterfaceLuid;           // 8 bytes

    [FieldOffset(40)]
    public uint InterfaceIndex;           // 4 bytes

    [FieldOffset(44)]
    public NL_PREFIX_ORIGIN PrefixOrigin; // 4 bytes

    [FieldOffset(48)]
    public NL_SUFFIX_ORIGIN SuffixOrigin; // 4 bytes

    [FieldOffset(52)]
    public uint ValidLifetime;            // 4 bytes

    [FieldOffset(56)]
    public uint PreferredLifetime;        // 4 bytes

    [FieldOffset(60)]
    public byte OnLinkPrefixLength;       // 1 byte

    [FieldOffset(61)]
    [MarshalAs(UnmanagedType.U1)]
    public bool SkipAsSource;             // 1 byte

    // 2 bytes padding (62-63)

    [FieldOffset(64)]
    public NL_DAD_STATE DadState;         // 4 bytes

    [FieldOffset(68)]
    public uint ScopeId;                  // 4 bytes

    [FieldOffset(72)]
    public long CreationTimeStamp;        // 8 bytes (72-79)
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
    public uint Reserved; // 64 位系统上的对齐填充
    // 后续紧跟 MIB_IPFORWARD_ROW2 数组
}

/// <summary>
/// 单播 IP 地址表头结构（MIB_UNICASTIPADDRESS_TABLE）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MIB_UNICASTIPADDRESS_TABLE
{
    public uint NumEntries;
    public uint Reserved; // 64 位系统上的对齐填充
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
