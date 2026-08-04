using System.Text;
using NetworkManagerDbus.Dto;

namespace NetworkManagerDbus.Mappers;

public static class ConnectionSettingsMapper
{
    public static NetworkConfigurationDto Map(IDictionary<string, IDictionary<string, object>> settings)
    {
        var dto = new NetworkConfigurationDto();

        if (settings.TryGetValue("connection", out var connection))
        {
            dto.ConnectionId = GetString(connection, "id") ?? string.Empty;
            dto.ConnectionType = GetString(connection, "type") ?? dto.ConnectionType;
            dto.Uuid = GetString(connection, "uuid");
            dto.InterfaceName = GetString(connection, "interface-name");
            dto.MasterConnectionUuid = GetString(connection, "master");
            dto.SlaveType = GetString(connection, "slave-type");
            dto.ConnectionOptions = new ConnectionOptionsDto
            {
                Autoconnect = GetBool(connection, "autoconnect") ?? true,
                AutoconnectPriority = GetInt(connection, "autoconnect-priority"),
                AutoconnectRetries = GetInt(connection, "autoconnect-retries"),
            };
        }

        if (settings.TryGetValue("ipv4", out var ipv4))
            dto.Ipv4 = MapIp(ipv4, isIpv4: true);

        if (settings.TryGetValue("ipv6", out var ipv6))
            dto.Ipv6 = MapIp(ipv6, isIpv4: false);

        if (settings.TryGetValue("bridge", out var bridge))
            dto.Bridge = new BridgeConfigurationDto { Stp = GetBool(bridge, "stp"), MulticastSnooping = GetBool(bridge, "multicast-snooping") };

        if (settings.TryGetValue("802-11-wireless", out var wifi))
        {
            dto.Wifi = new WifiConfigurationDto
            {
                Ssid = DecodeSsid(GetValue(wifi, "ssid")),
                Hidden = GetBool(wifi, "hidden") ?? false,
                Band = GetString(wifi, "band"),
                Channel = GetInt(wifi, "channel"),
                Mode = ParseWifiMode(GetString(wifi, "mode")),
                ClonedMacAddress = GetString(wifi, "cloned-mac-address"),
            };
        }

        if (settings.TryGetValue("802-11-wireless-security", out var wifiSecurity))
        {
            dto.WifiSecurity = new WifiSecurityDto
            {
                PreSharedKey = GetString(wifiSecurity, "psk"),
                KeyManagement = GetString(wifiSecurity, "key-mgmt"),
            };
        }

        if (settings.TryGetValue("gsm", out var gsm))
        {
            dto.Gsm = new GsmConfigurationDto
            {
                Apn = GetString(gsm, "apn"),
            };
        }

        return dto;
    }

    private static IpConfigurationDto MapIp(IDictionary<string, object> section, bool isIpv4)
    {
        var methodRaw = GetString(section, "method") ?? (isIpv4 ? "auto" : "auto");
        var method = ParseIpMethod(methodRaw, isIpv4);

        var ip = new IpConfigurationDto
        {
            Method = method,
            Enabled = method is not (IpMethod.Disabled or IpMethod.Ignore),
            Gateway = GetString(section, "gateway"),
            MayFail = GetBool(section, "may-fail"),
            AddrGenMode = !isIpv4 ? GetString(section, "addr-gen-mode") : null,
            LinkLocal = isIpv4 ? GetInt(section, "link-local") : null,
        };

        var addressData = GetEnumerable(GetValue(section, "address-data"));
        foreach (var item in addressData)
        {
            if (item is not IDictionary<string, object> map)
                continue;

            var address = GetString(map, "address");
            var prefix = GetInt(map, "prefix");
            if (string.IsNullOrWhiteSpace(address))
                continue;

            ip.Addresses.Add(new IpAddressDto
            {
                Address = address,
                Prefix = prefix,
                Netmask = prefix.HasValue ? PrefixToNetmask(prefix.Value) : null,
            });
        }

        var dnsData = GetEnumerable(GetValue(section, "dns-data"));
        foreach (var item in dnsData)
        {
            var dns = item?.ToString();
            if (!string.IsNullOrWhiteSpace(dns))
                ip.DnsServers.Add(dns);
        }

        var routeData = GetEnumerable(GetValue(section, "route-data"));
        foreach (var item in routeData)
        {
            if (item is not IDictionary<string, object> map)
                continue;

            var dest = GetString(map, "dest");
            var prefix = GetInt(map, "prefix");
            if (string.IsNullOrWhiteSpace(dest) || prefix is null)
                continue;

            ip.Routes.Add(new IpRouteDto
            {
                Destination = dest,
                Prefix = prefix.Value,
                NextHop = GetString(map, "next-hop"),
                Metric = GetUInt(map, "metric"),
            });
        }

        return ip;
    }

    private static IpMethod ParseIpMethod(string method, bool isIpv4)
    {
        return method switch
        {
            "auto" => IpMethod.Auto,
            "manual" => IpMethod.Manual,
            "disabled" => IpMethod.Disabled,
            "link-local" => IpMethod.LinkLocal,
            "shared" => isIpv4 ? IpMethod.Shared : IpMethod.Auto,
            "ignore" => IpMethod.Ignore,
            _ => IpMethod.Auto,
        };
    }

    private static WifiProfileMode ParseWifiMode(string? value)
        => string.Equals(value, "ap", StringComparison.OrdinalIgnoreCase)
            ? WifiProfileMode.AccessPoint
            : WifiProfileMode.Client;

    private static string DecodeSsid(object? raw)
    {
        if (raw is null)
            return string.Empty;
        if (raw is byte[] bytes)
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        if (raw is string str)
            return str;

        return raw.ToString() ?? string.Empty;
    }

    private static object? GetValue(IDictionary<string, object> section, string key)
        => section.TryGetValue(key, out var value) ? value : null;

    private static string? GetString(IDictionary<string, object> section, string key)
        => GetString(GetValue(section, key));

    private static string? GetString(object? value)
        => value switch
        {
            null => null,
            string s => s,
            _ => value.ToString(),
        };

    private static bool? GetBool(IDictionary<string, object> section, string key)
        => GetBool(GetValue(section, key));

    private static bool? GetBool(object? value)
    {
        return value switch
        {
            bool b => b,
            _ when bool.TryParse(value?.ToString(), out var parsed) => parsed,
            _ => null,
        };
    }

    private static int? GetInt(IDictionary<string, object> section, string key)
        => GetInt(GetValue(section, key));

    private static int? GetInt(object? value)
    {
        return value switch
        {
            byte b => b,
            short s => s,
            int i => i,
            long l when l is >= int.MinValue and <= int.MaxValue => (int)l,
            uint u when u <= int.MaxValue => (int)u,
            _ when int.TryParse(value?.ToString(), out var parsed) => parsed,
            _ => null,
        };
    }

    private static uint? GetUInt(IDictionary<string, object> section, string key)
        => GetUInt(GetValue(section, key));

    private static uint? GetUInt(object? value)
    {
        return value switch
        {
            byte b => b,
            ushort s => s,
            uint u => u,
            int i when i >= 0 => (uint)i,
            long l when l is >= uint.MinValue and <= uint.MaxValue => (uint)l,
            _ when uint.TryParse(value?.ToString(), out var parsed) => parsed,
            _ => null,
        };
    }

    private static string? PrefixToNetmask(int prefix)
    {
        if (prefix < 0 || prefix > 32) return null;
        uint mask = prefix == 0 ? 0u : (0xFFFFFFFFu << (32 - prefix));
        return $"{(mask >> 24) & 0xFF}.{(mask >> 16) & 0xFF}.{(mask >> 8) & 0xFF}.{mask & 0xFF}";
    }

    private static IEnumerable<object?> GetEnumerable(object? value)
    {
        if (value is null)
            yield break;

        if (value is Array arr)
        {
            foreach (var item in arr)
                yield return item;
            yield break;
        }

        if (value is IEnumerable<object?> en)
        {
            foreach (var item in en)
                yield return item;
        }
    }
}
