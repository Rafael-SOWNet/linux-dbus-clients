using NetworkManagerDbus.Dto;

namespace NetworkManagerDbus.Builders;

public static class WifiProfileFactory
{
    public static NetworkConfigurationDto CreateClientProfile(WifiClientProfileRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.InterfaceName))
            throw new ArgumentException("InterfaceName is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ConnectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Ssid))
            throw new ArgumentException("Ssid is required.", nameof(request));

        return new NetworkConfigurationDto
        {
            ConnectionType = "802-11-wireless",
            ConnectionId = request.ConnectionId,
            InterfaceName = request.InterfaceName,
            ConnectionOptions = new ConnectionOptionsDto
            {
                Autoconnect = true,
            },
            Wifi = new WifiConfigurationDto
            {
                Mode = WifiProfileMode.Client,
                Ssid = request.Ssid,
                Hidden = request.Hidden,
            },
            WifiSecurity = new WifiSecurityDto
            {
                PreSharedKey = request.PreSharedKey,
            },
            Ipv4 = new IpConfigurationDto
            {
                Enabled = true,
                Method = IpMethod.Auto,
                LinkLocal = 1,
            },
            Ipv6 = request.EnableIpv6
                ? new IpConfigurationDto { Enabled = true, Method = IpMethod.Auto }
                : new IpConfigurationDto { Enabled = false, Method = IpMethod.Ignore },
        };
    }

    public static NetworkConfigurationDto CreateAccessPointProfile(WifiAccessPointProfileRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.InterfaceName))
            throw new ArgumentException("InterfaceName is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ConnectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Ssid))
            throw new ArgumentException("Ssid is required.", nameof(request));

        if (!string.IsNullOrWhiteSpace(request.BridgeMasterConnectionUuid))
        {
            return new NetworkConfigurationDto
            {
                ConnectionType = "802-11-wireless",
                ConnectionId = request.ConnectionId,
                InterfaceName = request.InterfaceName,
                ConnectionOptions = new ConnectionOptionsDto
                {
                    Autoconnect = true,
                    AutoconnectPriority = 100,
                },
                Wifi = new WifiConfigurationDto
                {
                    Mode = WifiProfileMode.AccessPoint,
                    Ssid = request.Ssid,
                    Band = request.Band,
                    Channel = request.Channel,
                    ClonedMacAddress = request.Bssid,
                },
                WifiSecurity = string.IsNullOrWhiteSpace(request.PreSharedKey) ? null : new WifiSecurityDto
                {
                    PreSharedKey = request.PreSharedKey,
                    KeyManagement = "sae",
                },
                MasterConnectionUuid = request.BridgeMasterConnectionUuid,
                SlaveType = "bridge",
                Ipv4 = new IpConfigurationDto
                {
                    Enabled = false,
                    Method = IpMethod.Disabled,
                },
                Ipv6 = new IpConfigurationDto
                {
                    Enabled = false,
                    Method = IpMethod.Ignore,
                },
            };
        }

        return new NetworkConfigurationDto
        {
            ConnectionType = "802-11-wireless",
            ConnectionId = request.ConnectionId,
            InterfaceName = request.InterfaceName,
            ConnectionOptions = new ConnectionOptionsDto
            {
                Autoconnect = true,
                AutoconnectPriority = 100,
            },
            Wifi = new WifiConfigurationDto
            {
                Mode = WifiProfileMode.AccessPoint,
                Ssid = request.Ssid,
                Band = request.Band,
                Channel = request.Channel,
                ClonedMacAddress = request.Bssid,
            },
            WifiSecurity = string.IsNullOrWhiteSpace(request.PreSharedKey) ? null : new WifiSecurityDto
            {
                PreSharedKey = request.PreSharedKey,
                KeyManagement = "sae",
            },
            Ipv4 = new IpConfigurationDto
            {
                Enabled = true,
                Method = IpMethod.Shared,
            },
            Ipv6 = request.EnableIpv6
                ? new IpConfigurationDto { Enabled = true, Method = IpMethod.Auto }
                : new IpConfigurationDto { Enabled = false, Method = IpMethod.Ignore },
        };
    }
}
