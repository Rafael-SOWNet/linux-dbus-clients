using NetworkManagerDbus.Dto;

namespace NetworkManagerDbus.Builders;

public static class EthernetProfileFactory
{
    public static IReadOnlyList<NetworkConfigurationDto> Create(EthernetProfileRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.InterfaceName))
            throw new ArgumentException("InterfaceName is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.BaseConnectionId))
            throw new ArgumentException("BaseConnectionId is required.", nameof(request));

        var profiles = new List<NetworkConfigurationDto>();

        profiles.Add(request.Mode switch
        {
            EthernetAddressingMode.Dhcp => CreateDhcpProfile(request),
            EthernetAddressingMode.Static => CreateStaticProfile(request),
            EthernetAddressingMode.LinkLocal => CreateLinkLocalProfile(request),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Mode), request.Mode, null),
        });

        if (request.AddLinkLocalFallbackProfile && request.Mode != EthernetAddressingMode.LinkLocal)
            profiles.Add(CreateLinkLocalFallbackProfile(request));

        return profiles;
    }

    private static NetworkConfigurationDto CreateDhcpProfile(EthernetProfileRequestDto request)
    {
        return CreateBase(request, request.BaseConnectionId, request.PrimaryAutoconnectPriority, new IpConfigurationDto
        {
            Enabled = true,
            Method = IpMethod.Auto,
            LinkLocal = 1,
            // Allow fallback profile to activate if DHCP cannot be obtained.
            MayFail = true,
        });
    }

    private static NetworkConfigurationDto CreateStaticProfile(EthernetProfileRequestDto request)
    {
        if (request.StaticAddresses.Count == 0)
            throw new InvalidOperationException("Static mode requires at least one static address.");

        return CreateBase(request, request.BaseConnectionId, request.PrimaryAutoconnectPriority, new IpConfigurationDto
        {
            Enabled = true,
            Method = IpMethod.Manual,
            Addresses = request.StaticAddresses,
            Gateway = request.Gateway,
            DnsServers = request.DnsServers,
            Routes = request.Routes,
        });
    }

    private static NetworkConfigurationDto CreateLinkLocalProfile(EthernetProfileRequestDto request)
    {
        return CreateBase(request, request.BaseConnectionId, request.PrimaryAutoconnectPriority, new IpConfigurationDto
        {
            Enabled = true,
            Method = IpMethod.LinkLocal,
        });
    }

    private static NetworkConfigurationDto CreateLinkLocalFallbackProfile(EthernetProfileRequestDto request)
    {
        return CreateBase(request, request.BaseConnectionId + "-ll", request.FallbackAutoconnectPriority, new IpConfigurationDto
        {
            Enabled = true,
            Method = IpMethod.LinkLocal,
        });
    }

    private static NetworkConfigurationDto CreateBase(EthernetProfileRequestDto request, string id, int priority, IpConfigurationDto ipv4)
    {
        return new NetworkConfigurationDto
        {
            ConnectionType = "802-3-ethernet",
            ConnectionId = id,
            InterfaceName = request.InterfaceName,
            ConnectionOptions = new ConnectionOptionsDto
            {
                Autoconnect = true,
                AutoconnectPriority = priority,
            },
            Ipv4 = ipv4,
            Ipv6 = request.EnableIpv6
                ? new IpConfigurationDto { Enabled = true, Method = IpMethod.Auto }
                : new IpConfigurationDto { Enabled = false, Method = IpMethod.Ignore },
        };
    }
}
