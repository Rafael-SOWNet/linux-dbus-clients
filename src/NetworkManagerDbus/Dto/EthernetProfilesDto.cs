namespace NetworkManagerDbus.Dto;

public enum EthernetAddressingMode
{
    Dhcp,
    Static,
    LinkLocal,
}

public sealed class EthernetProfileRequestDto
{
    public string InterfaceName { get; set; } = "eth0";
    public string BaseConnectionId { get; set; } = "eth0";

    public EthernetAddressingMode Mode { get; set; } = EthernetAddressingMode.Dhcp;
    public bool EnableIpv6 { get; set; } = true;

    // When enabled, a secondary link-local profile is generated with lower autoconnect priority.
    public bool AddLinkLocalFallbackProfile { get; set; }

    // Required for static mode.
    public List<IpAddressDto> StaticAddresses { get; set; } = [];
    public string? Gateway { get; set; }
    public List<string> DnsServers { get; set; } = [];
    public List<IpRouteDto> Routes { get; set; } = [];

    // Autoconnect priority for the primary profile.
    public int PrimaryAutoconnectPriority { get; set; } = 100;
    public int FallbackAutoconnectPriority { get; set; } = -100;
}
