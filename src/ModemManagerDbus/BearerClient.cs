using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class BearerClient
{
    private readonly IBearer _proxy;
    private readonly Connection _connection;
    private readonly ObjectPath _path;

    public BearerClient(Connection connection, ObjectPath objectPath)
    {
        _connection = connection;
        _path = objectPath;
        _proxy = connection.CreateProxy<IBearer>(MmConstants.ModemManagerInterface, objectPath);
    }

    public ObjectPath ObjectPath => _path;

    public Task ConnectAsync() => _proxy.ConnectAsync();
    public Task DisconnectAsync() => _proxy.DisconnectAsync();

    public Task<string> GetInterfaceAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Interface").WaitAsync(cancellationToken);

    public Task<bool> GetConnectedAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<bool>("Connected").WaitAsync(cancellationToken);

    public Task<bool> GetSuspendedAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<bool>("Suspended").WaitAsync(cancellationToken);

    public async Task<BearerIpConfig> GetIp4ConfigAsync(CancellationToken cancellationToken = default)
    {
        var map = await _proxy.GetAsync<IDictionary<string, object>>("Ip4Config").WaitAsync(cancellationToken);
        var cfg = ParseBearerIpConfig(map, MMBearerIpFamily.Ipv4);
        return cfg;
    }

    public async Task<BearerIpConfig> GetIp6ConfigAsync(CancellationToken cancellationToken = default)
    {
        var map = await _proxy.GetAsync<IDictionary<string, object>>("Ip6Config").WaitAsync(cancellationToken);
        var cfg = ParseBearerIpConfig(map, MMBearerIpFamily.Ipv6);
        return cfg;
    }

    private static BearerIpConfig ParseBearerIpConfig(IDictionary<string, object> map, MMBearerIpFamily ipFamily)
    {
        var cfg = new BearerIpConfig
        {
            IpFamily = ipFamily,
            Method = MMBearerIpMethod.Unknown,
            Address = string.Empty,
            Prefix = 0,
            Dns1 = string.Empty,
            Dns2 = string.Empty,
            Dns3 = string.Empty,
            Gateway = string.Empty,
            Mtu = 0,
        };

        if (map.TryGetValue("method", out var methodObj) && DbusConvert.TryToUInt32(methodObj, out var method))
            cfg.Method = (MMBearerIpMethod)method;
        if (map.TryGetValue("address", out var addressObj) && DbusConvert.TryToString(addressObj, out var addr))
            cfg.Address = addr;
        if (map.TryGetValue("prefix", out var prefixObj) && DbusConvert.TryToUInt32(prefixObj, out var prefix))
            cfg.Prefix = prefix;
        if (map.TryGetValue("dns1", out var dns1Obj) && DbusConvert.TryToString(dns1Obj, out var dns1))
            cfg.Dns1 = dns1;
        if (map.TryGetValue("dns2", out var dns2Obj) && DbusConvert.TryToString(dns2Obj, out var dns2))
            cfg.Dns2 = dns2;
        if (map.TryGetValue("dns3", out var dns3Obj) && DbusConvert.TryToString(dns3Obj, out var dns3))
            cfg.Dns3 = dns3;
        if (map.TryGetValue("gateway", out var gatewayObj) && DbusConvert.TryToString(gatewayObj, out var gw))
            cfg.Gateway = gw;
        if (map.TryGetValue("mtu", out var mtuObj) && DbusConvert.TryToUInt32(mtuObj, out var mtu))
            cfg.Mtu = mtu;

        return cfg;
    }

    public async Task<BearerStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var map = await _proxy.GetAsync<IDictionary<string, object>>("Stats").WaitAsync(cancellationToken);

        ulong rxBytes = 0;
        ulong txBytes = 0;
        uint duration = 0;

        if (map.TryGetValue("rx-bytes", out var rxObj) && DbusConvert.TryToUInt64(rxObj, out var rx))
            rxBytes = rx;
        if (map.TryGetValue("tx-bytes", out var txObj) && DbusConvert.TryToUInt64(txObj, out var tx))
            txBytes = tx;
        if (map.TryGetValue("duration", out var durObj) && DbusConvert.TryToUInt32(durObj, out var dur))
            duration = dur;

        return new BearerStats(rxBytes, txBytes, duration);
    }

    public Task<uint> GetIpTimeoutAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<uint>("IpTimeout").WaitAsync(cancellationToken);

    public async Task<MMBearerType> GetBearerTypeAsync(CancellationToken cancellationToken = default)
        => (MMBearerType)await _proxy.GetAsync<uint>("BearerType").WaitAsync(cancellationToken);

    public async Task<BearerProperty> GetPropertiesAsync(CancellationToken cancellationToken = default)
    {
        var map = await _proxy.GetAsync<IDictionary<string, object>>("Properties").WaitAsync(cancellationToken);
        var props = new BearerProperty();

        if (map.TryGetValue("apn", out var apnObj) && DbusConvert.TryToString(apnObj, out var apn))
            props.apn = apn;
        if (map.TryGetValue("ip-type", out var ipTypeObj) && DbusConvert.TryToUInt32(ipTypeObj, out var ipType))
            props.ip_type = (MMBearerIpFamily)ipType;
        if (map.TryGetValue("allowed-auth", out var allowedAuthObj) && DbusConvert.TryToUInt32(allowedAuthObj, out var allowedAuth))
            props.allowed_auth = (MMBearerAllowedAuth)allowedAuth;
        if (map.TryGetValue("user", out var userObj) && DbusConvert.TryToString(userObj, out var user))
            props.user = user;
        if (map.TryGetValue("password", out var passwordObj) && DbusConvert.TryToString(passwordObj, out var password))
            props.password = password;
        if (map.TryGetValue("allow-roaming", out var allowRoamingObj) && DbusConvert.TryToBool(allowRoamingObj, out var allowRoaming))
            props.allow_roaming = allowRoaming;
        if (map.TryGetValue("rm-protocol", out var rmProtocolObj) && DbusConvert.TryToUInt32(rmProtocolObj, out var rmProtocol))
            props.rm_protocol = (MMModemCdmaRmProtocol)rmProtocol;
        if (map.TryGetValue("number", out var numberObj) && DbusConvert.TryToString(numberObj, out var number))
            props.number = number;

        return props;
    }
}
