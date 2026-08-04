using System.Net;
using System.Text;
using NetworkManagerDbus.Dto;

namespace NetworkManagerDbus.Builders;

public static class ConnectionSettingsBuilder
{
    public static IDictionary<string, IDictionary<string, object>> Build(NetworkConfigurationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.ConnectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(dto));

        var settings = new Dictionary<string, IDictionary<string, object>>(StringComparer.Ordinal)
        {
            ["connection"] = BuildConnectionSection(dto),
            ["ipv4"] = BuildIpSection(dto.Ipv4, isIpv4: true),
            ["ipv6"] = BuildIpSection(dto.Ipv6, isIpv4: false),
        };

        if (dto.Bridge is not null)
        {
            var bridge = new Dictionary<string, object>(StringComparer.Ordinal);
            if (dto.Bridge.Stp.HasValue)
                bridge["stp"] = dto.Bridge.Stp.Value;
            if (dto.Bridge.MulticastSnooping.HasValue)
                bridge["multicast-snooping"] = dto.Bridge.MulticastSnooping.Value;
            settings["bridge"] = bridge;
        }

        if (dto.Wifi is not null)
        {
            settings["802-11-wireless"] = BuildWifiSection(dto.Wifi);

            var securitySection = BuildWifiSecuritySection(dto.WifiSecurity);
            if (securitySection is not null)
                settings["802-11-wireless-security"] = securitySection;
        }

        if (dto.Gsm is not null)
            settings["gsm"] = BuildGsmSection(dto.Gsm);

        return settings;
    }

    private static IDictionary<string, object> BuildGsmSection(GsmConfigurationDto gsm)
    {
        if (string.IsNullOrWhiteSpace(gsm.Apn))
            throw new InvalidOperationException("GSM APN is required when GSM configuration is set.");

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["apn"] = gsm.Apn,
        };
    }

    private static IDictionary<string, object> BuildWifiSection(WifiConfigurationDto wifi)
    {
        if (string.IsNullOrWhiteSpace(wifi.Ssid))
            throw new InvalidOperationException("Wi-Fi SSID is required when Wi-Fi configuration is set.");

        var section = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["ssid"] = Encoding.UTF8.GetBytes(wifi.Ssid),
            ["mode"] = wifi.Mode == WifiProfileMode.AccessPoint ? "ap" : "infrastructure",
        };

        if (wifi.Hidden)
            section["hidden"] = true;
        if (!string.IsNullOrWhiteSpace(wifi.Band))
            section["band"] = wifi.Band;
        if (wifi.Channel.HasValue)
            section["channel"] = wifi.Channel.Value;
        if (!string.IsNullOrWhiteSpace(wifi.ClonedMacAddress))
            section["cloned-mac-address"] = wifi.ClonedMacAddress;

        return section;
    }

    private static IDictionary<string, object>? BuildWifiSecuritySection(WifiSecurityDto? security)
    {
        if (security is null || string.IsNullOrWhiteSpace(security.PreSharedKey))
            return null;

        var keyMgmt = string.IsNullOrWhiteSpace(security.KeyManagement) ? "wpa-psk" : security.KeyManagement;
        var section = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["key-mgmt"] = keyMgmt,
            ["psk"] = security.PreSharedKey,
        };

        // WPA3-SAE requires Protected Management Frames (pmf=2).
        if (string.Equals(keyMgmt, "sae", StringComparison.OrdinalIgnoreCase))
            section["pmf"] = 2;

        return section;
    }

    private static IDictionary<string, object> BuildConnectionSection(NetworkConfigurationDto dto)
    {
        var section = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["id"] = dto.ConnectionId,
            ["type"] = dto.ConnectionType,
            ["uuid"] = string.IsNullOrWhiteSpace(dto.Uuid) ? Guid.NewGuid().ToString() : dto.Uuid,
            ["autoconnect"] = dto.ConnectionOptions.Autoconnect,
        };

        if (!string.IsNullOrWhiteSpace(dto.InterfaceName))
            section["interface-name"] = dto.InterfaceName;
        if (dto.ConnectionOptions.AutoconnectPriority.HasValue)
            section["autoconnect-priority"] = dto.ConnectionOptions.AutoconnectPriority.Value;
        if (dto.ConnectionOptions.AutoconnectRetries.HasValue)
            section["autoconnect-retries"] = dto.ConnectionOptions.AutoconnectRetries.Value;
        if (!string.IsNullOrWhiteSpace(dto.MasterConnectionUuid))
            section["master"] = dto.MasterConnectionUuid;
        if (!string.IsNullOrWhiteSpace(dto.SlaveType))
            section["slave-type"] = dto.SlaveType;

        return section;
    }

    private static IDictionary<string, object> BuildIpSection(IpConfigurationDto ip, bool isIpv4)
    {
        var section = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["method"] = ResolveMethod(ip, isIpv4),
        };

        if (ip.MayFail.HasValue)
            section["may-fail"] = ip.MayFail.Value;

        if (isIpv4 && ip.LinkLocal.HasValue)
            section["link-local"] = ip.LinkLocal.Value;

        if (!isIpv4 && !string.IsNullOrWhiteSpace(ip.AddrGenMode))
            section["addr-gen-mode"] = ResolveAddrGenMode(ip.AddrGenMode);

        if (ip.Addresses.Count > 0)
        {
            var addressData = new List<IDictionary<string, object>>(ip.Addresses.Count);
            foreach (var address in ip.Addresses)
            {
                if (string.IsNullOrWhiteSpace(address.Address))
                    continue;

                var prefix = ResolvePrefix(address, isIpv4);
                addressData.Add(new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["address"] = address.Address,
                    ["prefix"] = (uint)prefix,
                });
            }

            if (addressData.Count > 0)
                section["address-data"] = addressData.ToArray();
        }

        if (!string.IsNullOrWhiteSpace(ip.Gateway))
            section["gateway"] = ip.Gateway;

        if (ip.DnsServers.Count > 0)
            section["dns-data"] = ip.DnsServers.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        if (ip.Routes.Count > 0)
        {
            var routeData = new List<IDictionary<string, object>>(ip.Routes.Count);
            foreach (var route in ip.Routes)
            {
                if (string.IsNullOrWhiteSpace(route.Destination))
                    continue;

                var routeMap = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["dest"] = route.Destination,
                    ["prefix"] = (uint)route.Prefix,
                };

                if (!string.IsNullOrWhiteSpace(route.NextHop))
                    routeMap["next-hop"] = route.NextHop;
                if (route.Metric.HasValue)
                    routeMap["metric"] = (uint)route.Metric.Value;

                routeData.Add(routeMap);
            }

            if (routeData.Count > 0)
                section["route-data"] = routeData.ToArray();
        }

        return section;
    }

    private static int ResolvePrefix(IpAddressDto address, bool isIpv4)
    {
        if (address.Prefix.HasValue)
            return address.Prefix.Value;

        if (isIpv4 && !string.IsNullOrWhiteSpace(address.Netmask))
            return NetmaskToPrefix(address.Netmask);

        throw new InvalidOperationException("Address prefix is required for manual IP addresses.");
    }

    private static int NetmaskToPrefix(string netmask)
    {
        if (!IPAddress.TryParse(netmask, out var parsed) || parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            throw new InvalidOperationException($"Invalid IPv4 netmask: '{netmask}'.");

        var bytes = parsed.GetAddressBytes();
        var bits = 0;
        var hitZero = false;

        foreach (var b in bytes)
        {
            for (var i = 7; i >= 0; i--)
            {
                var one = (b & (1 << i)) != 0;
                if (hitZero && one)
                    throw new InvalidOperationException($"Non-contiguous IPv4 netmask: '{netmask}'.");

                if (one)
                    bits++;
                else
                    hitZero = true;
            }
        }

        return bits;
    }

    private static string ResolveMethod(IpConfigurationDto ip, bool isIpv4)
    {
        if (!ip.Enabled)
            return isIpv4 ? "disabled" : "ignore";

        return ip.Method switch
        {
            IpMethod.Auto => "auto",
            IpMethod.Manual => "manual",
            IpMethod.Disabled => isIpv4 ? "disabled" : "ignore",
            IpMethod.LinkLocal => "link-local",
            IpMethod.Shared => isIpv4 ? "shared" : "auto",
            IpMethod.Ignore => isIpv4 ? "disabled" : "ignore",
            _ => isIpv4 ? "auto" : "auto",
        };
    }

    // NetworkManager expects addr-gen-mode as integer (NMSettingIP6ConfigAddrGenMode enum):
    //   0 = eui64, 1 = stable-privacy, 2 = default, 3 = default-or-eui64
    private static int ResolveAddrGenMode(string value)
    {
        if (int.TryParse(value, out var i))
            return i;
        return value.ToLowerInvariant() switch
        {
            "eui64" => 0,
            "stable-privacy" => 1,
            "default" => 2,
            "default-or-eui64" => 3,
            _ => 0,
        };
    }
}
