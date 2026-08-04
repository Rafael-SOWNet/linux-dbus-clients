namespace NetworkManagerDbus.Dto;

public enum IpMethod
{
    Auto,
    Manual,
    Disabled,
    LinkLocal,
    Shared,
    Ignore,
}

public sealed class ConnectionOptionsDto
{
    public bool Autoconnect { get; set; } = true;
    public int? AutoconnectPriority { get; set; }
    public int? AutoconnectRetries { get; set; }
}

public sealed class IpAddressDto
{
    public string Address { get; set; } = string.Empty;
    public int? Prefix { get; set; }
    public string? Netmask { get; set; }
}

public sealed class IpRouteDto
{
    public string Destination { get; set; } = string.Empty;
    public int Prefix { get; set; }
    public string? NextHop { get; set; }
    public uint? Metric { get; set; }
}

public sealed class IpConfigurationDto
{
    public bool Enabled { get; set; } = true;
    public IpMethod Method { get; set; } = IpMethod.Auto;
    public List<IpAddressDto> Addresses { get; set; } = [];
    public string? Gateway { get; set; }
    public List<string> DnsServers { get; set; } = [];
    public List<IpRouteDto> Routes { get; set; } = [];
    public bool? MayFail { get; set; }
    public string? AddrGenMode { get; set; }
    // ipv4.link-local: 0=default, 1=enabled, 2=disabled, 3=auto (NM 1.40+)
    public int? LinkLocal { get; set; }
}

public sealed class BridgeConfigurationDto
{
    public bool? Stp { get; set; }
    public bool? MulticastSnooping { get; set; }
}

public enum WifiProfileMode
{
    Client,
    AccessPoint,
}

public sealed class WifiConfigurationDto
{
    public WifiProfileMode Mode { get; set; } = WifiProfileMode.Client;
    public string Ssid { get; set; } = string.Empty;
    public bool Hidden { get; set; }
    public string? Band { get; set; }
    public int? Channel { get; set; }
    // Sets the MAC address used for the connection (AP mode: becomes the BSSID).
    public string? ClonedMacAddress { get; set; }
}

public sealed class WifiSecurityDto
{
    // Empty/null means open network.
    public string? PreSharedKey { get; set; }
    // "wpa-psk" (WPA2, default) or "sae" (WPA3).
    public string? KeyManagement { get; set; }
}

public sealed class GsmConfigurationDto
{
    public string? Apn { get; set; }
}

public sealed class NetworkConfigurationDto
{
    public string ConnectionType { get; set; } = "802-3-ethernet";
    public string ConnectionId { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    public string? InterfaceName { get; set; }

    public ConnectionOptionsDto ConnectionOptions { get; set; } = new();
    public IpConfigurationDto Ipv4 { get; set; } = new();
    public IpConfigurationDto Ipv6 { get; set; } = new();

    public BridgeConfigurationDto? Bridge { get; set; }
    public WifiConfigurationDto? Wifi { get; set; }
    public WifiSecurityDto? WifiSecurity { get; set; }
    public GsmConfigurationDto? Gsm { get; set; }

    public string? MasterConnectionUuid { get; set; }
    public string? SlaveType { get; set; }
}

public sealed class GsmProfileRequestDto
{
    public string ConnectionId { get; set; } = "modem-auto";
    public string? InterfaceName { get; set; }
    public string Apn { get; set; } = string.Empty;
    public bool EnableIpv6 { get; set; } = true;
    public int AutoconnectPriority { get; set; } = 150;
}

public sealed class WifiAccessPointDto
{
    public string DeviceInterface { get; set; } = string.Empty;
    public string ObjectPath { get; set; } = string.Empty;
    public string Ssid { get; set; } = string.Empty;
    public string? Bssid { get; set; }
    public byte Strength { get; set; }
    public uint? FrequencyMhz { get; set; }
    public bool Secured { get; set; }
}

public sealed class WifiClientProfileRequestDto
{
    public string InterfaceName { get; set; } = "wlan0";
    public string ConnectionId { get; set; } = "wifi-client";
    public string Ssid { get; set; } = string.Empty;
    public bool Hidden { get; set; }
    public string? PreSharedKey { get; set; }
    public bool EnableIpv6 { get; set; } = true;
}

public sealed class WifiAccessPointProfileRequestDto
{
    public string InterfaceName { get; set; } = "wlan0";
    public string ConnectionId { get; set; } = "wifi-ap";
    public string Ssid { get; set; } = string.Empty;
    public string? PreSharedKey { get; set; }
    public int? Channel { get; set; }
    public string? Band { get; set; }
    public bool EnableIpv6 { get; set; }
    public string? BridgeMasterConnectionUuid { get; set; }
    // Optional MAC address override; becomes the AP's BSSID in access-point mode.
    public string? Bssid { get; set; }
}

public sealed class InterfaceRuntimeStateDto
{
    public string InterfaceName { get; set; } = string.Empty;
    public bool HasIpv4Config { get; set; }
    public bool HasIpv6Config { get; set; }
    public string? Ipv4LinkLocalAddress { get; set; }
    public string? Ipv6LinkLocalAddress { get; set; }
}

public sealed class ConnectionSummaryDto
{
    public string ObjectPath { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    public string? Type { get; set; }
    public string? InterfaceName { get; set; }
    public bool? Autoconnect { get; set; }
}
