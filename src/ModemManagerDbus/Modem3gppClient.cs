using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class Modem3gppClient
{
    private readonly IModem3gpp _proxy;
    private NetworkScanResult _scanResults = new() { Recent = false };

    public Modem3gppClient(Connection connection, ObjectPath modemObjectPath)
    {
        _proxy = connection.CreateProxy<IModem3gpp>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public Task RegisterAsync(string operatorId) => _proxy.RegisterAsync(operatorId);

    public async Task<IReadOnlyList<Network3Gpp>> ScanAsync(CancellationToken cancellationToken = default)
    {
        // Go version stores a recent scan cache; we do the same per instance.
        var start = DateTimeOffset.UtcNow;
        var res = await _proxy.ScanAsync().WaitAsync(cancellationToken);
        var networks = new List<Network3Gpp>();

        foreach (var dict in res)
        {
            var status = MMModem3gppNetworkAvailability.Unknown;
            var operatorLong = string.Empty;
            var operatorShort = string.Empty;
            var operatorCode = string.Empty;
            var accessTechnology = MMModemAccessTechnology.Unknown;

            if (dict.TryGetValue("status", out var statusObj) && DbusConvert.TryToUInt32(statusObj, out var st))
                status = (MMModem3gppNetworkAvailability)st;
            if (dict.TryGetValue("operator-long", out var opLongObj) && DbusConvert.TryToString(opLongObj, out var opLong))
                operatorLong = opLong;
            if (dict.TryGetValue("operator-short", out var opShortObj) && DbusConvert.TryToString(opShortObj, out var opShort))
                operatorShort = opShort;
            if (dict.TryGetValue("operator-code", out var opCodeObj) && DbusConvert.TryToString(opCodeObj, out var opCode))
                operatorCode = opCode;
            if (dict.TryGetValue("access-technology", out var techObj) && DbusConvert.TryToUInt32(techObj, out var tech))
                accessTechnology = (MMModemAccessTechnology)tech;

            var mcc = string.Empty;
            var mnc = string.Empty;
            if (operatorCode.Length > 4)
            {
                mcc = operatorCode.Substring(0, 3);
                mnc = operatorCode.Substring(3);
            }

            networks.Add(new Network3Gpp(status, operatorLong, operatorShort, operatorCode, mcc, mnc, accessTechnology));
        }

        _scanResults = new NetworkScanResult
        {
            Recent = true,
            LastScan = DateTimeOffset.UtcNow,
            ScanDurationSeconds = (DateTimeOffset.UtcNow - start).TotalSeconds,
            Networks = networks,
        };

        return networks;
    }

    public Task RequestScanAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => ScanAsync(cancellationToken), cancellationToken);

    public NetworkScanResult GetScanResults()
    {
        if (_scanResults.Recent)
            return _scanResults;
        throw new InvalidOperationException("No recent scans.");
    }

    public Task<string> GetImeiAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Imei").WaitAsync(cancellationToken);

    public async Task<MMModem3gppRegistrationState> GetRegistrationStateAsync(CancellationToken cancellationToken = default)
        => (MMModem3gppRegistrationState)await _proxy.GetAsync<uint>("RegistrationState").WaitAsync(cancellationToken);

    public Task<string> GetOperatorCodeAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("OperatorCode").WaitAsync(cancellationToken);

    public async Task<string> GetMccAsync(CancellationToken cancellationToken = default)
    {
        var code = await GetOperatorCodeAsync(cancellationToken);
        return code.Length > 4 ? code.Substring(0, 3) : string.Empty;
    }

    public async Task<string> GetMncAsync(CancellationToken cancellationToken = default)
    {
        var code = await GetOperatorCodeAsync(cancellationToken);
        return code.Length > 4 ? code.Substring(3) : string.Empty;
    }

    public Task<string> GetOperatorNameAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("OperatorName").WaitAsync(cancellationToken);

    public async Task<IReadOnlyList<MMModem3gppFacility>> GetEnabledFacilityLocksAsync(CancellationToken cancellationToken = default)
    {
        var bitmask = await _proxy.GetAsync<uint>("EnabledFacilityLocks").WaitAsync(cancellationToken);
        return MMModem3gppFacilityExt.BitmaskToSlice(bitmask);
    }

    public Task SetEpsUeModeOperationAsync(MMModem3gppEpsUeModeOperation mode)
        => _proxy.SetEpsUeModeOperationAsync((uint)mode);

    public async Task<MMModem3gppEpsUeModeOperation> GetEpsUeModeOperationAsync(CancellationToken cancellationToken = default)
        => (MMModem3gppEpsUeModeOperation)await _proxy.GetAsync<uint>("EpsUeModeOperation").WaitAsync(cancellationToken);

    public async Task<IReadOnlyList<RawPcoData>> GetPcoAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _proxy.GetAsync<(uint sessionId, bool complete, byte[] rawData)[]>("Pco").WaitAsync(cancellationToken);
        return raw.Select(x => new RawPcoData(x.sessionId, x.complete, x.rawData)).ToArray();
    }

    public async Task<BearerClient> GetInitialEpsBearerAsync(Connection connection, CancellationToken cancellationToken = default)
    {
        var path = await _proxy.GetAsync<ObjectPath>("InitialEpsBearer").WaitAsync(cancellationToken);
        if (path.ToString() == "/")
            throw new InvalidOperationException("No initial EPS bearer.");
        return new BearerClient(connection, path);
    }

    public Task SetInitialEpsBearerSettingsAsync(BearerProperty property)
        => _proxy.SetInitialEpsBearerSettingsAsync(property);

    public async Task<BearerProperty> GetInitialEpsBearerSettingsAsync(CancellationToken cancellationToken = default)
    {
        var map = await _proxy.GetAsync<IDictionary<string, object>>("InitialEpsBearerSettings").WaitAsync(cancellationToken);
        if (map.Count < 1)
            throw new InvalidOperationException("No initial EPS bearer settings found.");

        var props = new BearerProperty();
        if (map.TryGetValue("apn", out var apnObj) && DbusConvert.TryToString(apnObj, out var apn))
            props.apn = apn;
        if (map.TryGetValue("ip-type", out var ipTypeObj) && DbusConvert.TryToUInt32(ipTypeObj, out var ipType))
            props.ip_type = (MMBearerIpFamily)ipType;
        if (map.TryGetValue("allowed-auth", out var authObj) && DbusConvert.TryToUInt32(authObj, out var auth))
            props.allowed_auth = (MMBearerAllowedAuth)auth;
        if (map.TryGetValue("user", out var userObj) && DbusConvert.TryToString(userObj, out var user))
            props.user = user;
        if (map.TryGetValue("password", out var passObj) && DbusConvert.TryToString(passObj, out var pass))
            props.password = pass;

        return props;
    }
}
