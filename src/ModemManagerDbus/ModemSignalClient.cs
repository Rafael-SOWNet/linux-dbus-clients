using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemSignalClient
{
    private readonly IModemSignal _proxy;

    public ModemSignalClient(Connection connection, ObjectPath modemObjectPath)
    {
        _proxy = connection.CreateProxy<IModemSignal>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public Task SetupAsync(uint rateSeconds) => _proxy.SetupAsync(rateSeconds);

    public Task<uint> GetRateAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<uint>("Rate").WaitAsync(cancellationToken);

    private static SignalProperty ConvertMapToSignalProperty(IDictionary<string, object> dict, MMSignalPropertyType type)
    {
        double GetDouble(string key)
            => dict.TryGetValue(key, out var obj) && obj is double d ? d : 0d;

        return new SignalProperty
        {
            Type = type,
            Rssi = GetDouble("rssi"),
            Ecio = GetDouble("ecio"),
            Sinr = GetDouble("sinr"),
            Io = GetDouble("io"),
            Rscp = GetDouble("rscp"),
            Rsrq = GetDouble("rsrq"),
            Rsrp = GetDouble("rsrp"),
            Snr = GetDouble("snr"),
        };
    }

    private static bool IsRssiSet(SignalProperty sp) => sp.Rssi != 0d;

    public async Task<SignalProperty> GetCdmaAsync(CancellationToken cancellationToken = default)
        => ConvertMapToSignalProperty(await _proxy.GetAsync<IDictionary<string, object>>("Cdma").WaitAsync(cancellationToken), MMSignalPropertyType.Cdma);

    public async Task<SignalProperty> GetEvdoAsync(CancellationToken cancellationToken = default)
        => ConvertMapToSignalProperty(await _proxy.GetAsync<IDictionary<string, object>>("Evdo").WaitAsync(cancellationToken), MMSignalPropertyType.Evdo);

    public async Task<SignalProperty> GetGsmAsync(CancellationToken cancellationToken = default)
        => ConvertMapToSignalProperty(await _proxy.GetAsync<IDictionary<string, object>>("Gsm").WaitAsync(cancellationToken), MMSignalPropertyType.Gsm);

    public async Task<SignalProperty> GetUmtsAsync(CancellationToken cancellationToken = default)
        => ConvertMapToSignalProperty(await _proxy.GetAsync<IDictionary<string, object>>("Umts").WaitAsync(cancellationToken), MMSignalPropertyType.Umts);

    public async Task<SignalProperty> GetLteAsync(CancellationToken cancellationToken = default)
        => ConvertMapToSignalProperty(await _proxy.GetAsync<IDictionary<string, object>>("Lte").WaitAsync(cancellationToken), MMSignalPropertyType.Lte);

    public async Task<IReadOnlyList<SignalProperty>> GetCurrentSignalsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<SignalProperty>();

        var cdma = await GetCdmaAsync(cancellationToken);
        if (IsRssiSet(cdma)) result.Add(cdma);

        var evdo = await GetEvdoAsync(cancellationToken);
        if (IsRssiSet(evdo)) result.Add(evdo);

        var gsm = await GetGsmAsync(cancellationToken);
        if (IsRssiSet(gsm)) result.Add(gsm);

        var umts = await GetUmtsAsync(cancellationToken);
        if (IsRssiSet(umts)) result.Add(umts);

        var lte = await GetLteAsync(cancellationToken);
        if (IsRssiSet(lte)) result.Add(lte);

        return result;
    }
}
