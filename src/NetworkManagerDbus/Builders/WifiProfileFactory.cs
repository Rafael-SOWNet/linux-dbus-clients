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

        // An open access point is not an allowed configuration. Both AP variants put the
        // device on the network edge - hotspot serves its own DHCP/NAT subnet, and the
        // bridge variant puts associated clients straight onto the wired segment - so an
        // unauthenticated AP exposes every service reachable through the host firewall to
        // anyone in radio range. Previously a blank key silently produced WifiSecurity =
        // null, i.e. an open AP, with no error anywhere.
        var preSharedKey = BuildAccessPointSecurity(request.PreSharedKey);

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
                WifiSecurity = preSharedKey,
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
            WifiSecurity = preSharedKey,
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

    /// <summary>
    /// Builds the security block for an access point, rejecting anything that would result in
    /// an open (unencrypted) AP. The length bounds are the SAE/WPA-PSK passphrase limits -
    /// NetworkManager would reject an out-of-range key too, but only after the profile
    /// round-trips over D-Bus, which surfaces as an opaque failure instead of a message the
    /// operator can act on.
    ///
    /// key-mgmt is "wpa-psk", not "sae": in AP mode NetworkManager treats "wpa-psk" as the
    /// WPA2/WPA3-Personal *transition* setting (it advertises both PSK and SAE AKMs, "WPA-PSK
    /// WPA-PSK-SHA256 SAE"), not WPA2-only - "sae" alone means WPA3-only, no fallback. A
    /// SAE-only hotspot failed to associate with a real phone (Galaxy A53 5G / Android 16,
    /// which does support WPA3) with a generic "Incorrect password" - confirmed the AP side had
    /// no errors (activated, correct PSK, no NetworkManager log failures), consistent with the
    /// known brcmfmac limitation where the Pi's onboard Wi-Fi can't complete a SAE handshake in
    /// AP mode even though it advertises support. Transition mode lets a WPA3-capable client use
    /// SAE when it works and falls back to plain WPA2-PSK (a completely different, unaffected
    /// code path) when it doesn't.
    /// </summary>
    private static WifiSecurityDto BuildAccessPointSecurity(string? preSharedKey)
    {
        if (string.IsNullOrWhiteSpace(preSharedKey))
            throw new ArgumentException(
                "A pre-shared key is required for access-point and hotspot mode; an open access point is not allowed.",
                nameof(preSharedKey));

        if (preSharedKey.Length is < 8 or > 63)
            throw new ArgumentException(
                "The access-point pre-shared key must be between 8 and 63 characters.",
                nameof(preSharedKey));

        return new WifiSecurityDto
        {
            PreSharedKey = preSharedKey,
            KeyManagement = "wpa-psk",
        };
    }
}
