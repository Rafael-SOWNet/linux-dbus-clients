using NetworkManagerDbus.Dto;

namespace NetworkManagerDbus.Tests;

/// <summary>
/// Representative configurations, one per shape the builder can emit. Deliberately synthetic and
/// generic: these are not copies of anyone's deployment, and nothing here should be read as a
/// recommended configuration.
/// </summary>
internal static class Profiles
{
    public static NetworkConfigurationDto BridgeStatic() => new()
    {
        ConnectionId = "br0",
        ConnectionType = "bridge",
        InterfaceName = "br0",
        Uuid = "11111111-1111-1111-1111-111111111111",
        Bridge = new BridgeConfigurationDto { Stp = false, MulticastSnooping = false },
        Ipv4 = new IpConfigurationDto
        {
            Method = IpMethod.Manual,
            Addresses = [new IpAddressDto { Address = "10.0.0.2", Prefix = 24 }],
            Gateway = "10.0.0.1",
            DnsServers = ["10.0.0.1"],
            Routes = [new IpRouteDto { Destination = "10.9.0.0", Prefix = 16, NextHop = "10.0.0.254", Metric = 50 }],
            LinkLocal = 1,
        },
        Ipv6 = new IpConfigurationDto { Method = IpMethod.Auto, AddrGenMode = "stable-privacy" },
    };

    public static NetworkConfigurationDto BridgeDhcp() => new()
    {
        ConnectionId = "br0",
        ConnectionType = "bridge",
        InterfaceName = "br0",
        Uuid = "22222222-2222-2222-2222-222222222222",
        Bridge = new BridgeConfigurationDto { Stp = false, MulticastSnooping = false },
        Ipv4 = new IpConfigurationDto { Method = IpMethod.Auto, LinkLocal = 1 },
        Ipv6 = new IpConfigurationDto { Method = IpMethod.Auto },
    };

    public static NetworkConfigurationDto EthernetSlave() => new()
    {
        ConnectionId = "eth0-slave",
        ConnectionType = "802-3-ethernet",
        InterfaceName = "eth0",
        Uuid = "33333333-3333-3333-3333-333333333333",
        MasterConnectionUuid = "11111111-1111-1111-1111-111111111111",
        SlaveType = "bridge",
        Ipv4 = new IpConfigurationDto { Method = IpMethod.Disabled },
        Ipv6 = new IpConfigurationDto { Method = IpMethod.Ignore },
    };

    public static NetworkConfigurationDto WifiClient() => new()
    {
        ConnectionId = "wifi-client",
        ConnectionType = "802-11-wireless",
        InterfaceName = "wlan0",
        Uuid = "44444444-4444-4444-4444-444444444444",
        Wifi = new WifiConfigurationDto { Mode = WifiProfileMode.Client, Ssid = "example-ssid" },
        WifiSecurity = new WifiSecurityDto { KeyManagement = "wpa-psk", PreSharedKey = "example-passphrase" },
        Ipv4 = new IpConfigurationDto { Method = IpMethod.Auto },
        Ipv6 = new IpConfigurationDto { Method = IpMethod.Auto },
    };

    public static NetworkConfigurationDto WifiAccessPointSae() => new()
    {
        ConnectionId = "wifi-ap",
        ConnectionType = "802-11-wireless",
        InterfaceName = "wlan0",
        Uuid = "55555555-5555-5555-5555-555555555555",
        Wifi = new WifiConfigurationDto
        {
            Mode = WifiProfileMode.AccessPoint,
            Ssid = "example-ap",
            Hidden = true,
            Band = "bg",
            Channel = 6,
        },
        WifiSecurity = new WifiSecurityDto { KeyManagement = "sae", PreSharedKey = "example-passphrase" },
        Ipv4 = new IpConfigurationDto { Method = IpMethod.Shared },
        Ipv6 = new IpConfigurationDto { Method = IpMethod.Ignore },
    };

    public static NetworkConfigurationDto Gsm() => new()
    {
        ConnectionId = "modem",
        ConnectionType = "gsm",
        Uuid = "66666666-6666-6666-6666-666666666666",
        Gsm = new GsmConfigurationDto { Apn = "example.apn" },
        Ipv4 = new IpConfigurationDto { Method = IpMethod.Auto },
        Ipv6 = new IpConfigurationDto { Method = IpMethod.Auto },
    };
}
