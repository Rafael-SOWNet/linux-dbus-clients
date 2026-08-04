using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemLocationClient
{
    private readonly IModemLocation _proxy;

    public ModemLocationClient(Connection connection, ObjectPath modemObjectPath)
    {
        _proxy = connection.CreateProxy<IModemLocation>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public Task SetupAsync(IEnumerable<MMModemLocationSource> sources, bool signalLocation)
    {
        var bitmask = MMModemLocationSourceExt.SliceToBitmask(sources);
        return _proxy.SetupAsync(bitmask, signalLocation);
    }

    public async Task<CurrentLocation> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        var dict = await _proxy.GetLocationAsync().WaitAsync(cancellationToken);
        return CreateLocation(dict);
    }

    public async Task<IReadOnlyList<MMModemLocationSource>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var bitmask = await _proxy.GetAsync<uint>("Capabilities").WaitAsync(cancellationToken);
        return MMModemLocationSourceExt.BitmaskToSlice(bitmask);
    }

    public async Task<IReadOnlyList<MMModemLocationAssistanceDataType>> GetSupportedAssistanceDataAsync(CancellationToken cancellationToken = default)
    {
        var bitmask = await _proxy.GetAsync<uint>("SupportedAssistanceData").WaitAsync(cancellationToken);
        return MMModemLocationAssistanceDataTypeExt.BitmaskToSlice(bitmask);
    }

    public async Task<IReadOnlyList<MMModemLocationSource>> GetEnabledLocationSourcesAsync(CancellationToken cancellationToken = default)
    {
        var bitmask = await _proxy.GetAsync<uint>("Enabled").WaitAsync(cancellationToken);
        return MMModemLocationSourceExt.BitmaskToSlice(bitmask);
    }

    public Task<bool> GetSignalsLocationAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<bool>("SignalsLocation").WaitAsync(cancellationToken);

    public async Task<CurrentLocation> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        var dict = await _proxy.GetAsync<IDictionary<uint, object>>("Location").WaitAsync(cancellationToken);
        return CreateLocation(dict);
    }

    public Task<string> GetSuplServerAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("SuplServer").WaitAsync(cancellationToken);

    public Task<IReadOnlyList<string>> GetAssistanceDataServersAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string[]>("AssistanceDataServers").WaitAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<string>)t.Result, cancellationToken);

    public Task<uint> GetGpsRefreshRateAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<uint>("GpsRefreshRate").WaitAsync(cancellationToken);

    public Task SetSuplServerAsync(string supl) => _proxy.SetSuplServerAsync(supl);

    public Task InjectAssistanceDataAsync(byte[] data) => _proxy.InjectAssistanceDataAsync(data);

    public Task SetGpsRefreshRateAsync(uint rateSeconds) => _proxy.SetGpsRefreshRateAsync(rateSeconds);

    private static CurrentLocation CreateLocation(IDictionary<uint, object> res)
    {
        var loc = new CurrentLocation();

        foreach (var kvp in res)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            var sType = MMModemLocationSourceExt.BitmaskToSlice(key);
            if (sType.Count < 1)
                continue;

            var locationType = sType[0];
            switch (locationType)
            {
                case MMModemLocationSource._3gppLacCi:
                    if (value is string s)
                    {
                        var parts = s.Split(',');
                        if (parts.Length == 5)
                        {
                            loc.ThreeGppLacCi = new ThreeGppLacCiLocation
                            {
                                Mcc = parts[0],
                                Mnc = parts[1],
                                Lac = parts[2],
                                Ci = parts[3],
                                Tac = parts[4],
                            };
                        }
                    }
                    break;

                case MMModemLocationSource.GpsRaw:
                    if (value is IDictionary<string, object> map)
                    {
                        DateTimeOffset utcTime = default;
                        double altitude = 0, lat = 0, lon = 0;

                        if (map.TryGetValue("utc-time", out var utcObj) && utcObj is string utcStr)
                        {
                            // Go code parses "HHmmss" and patches in the current date (because date is missing).
                            if (DateTime.TryParseExact(utcStr, "HHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var t))
                            {
                                var now = DateTimeOffset.UtcNow;
                                utcTime = new DateTimeOffset(now.Year, now.Month, now.Day, t.Hour, t.Minute, t.Second, TimeSpan.Zero);
                            }
                        }

                        if (map.TryGetValue("altitude", out var altObj) && altObj is double altD) altitude = altD;
                        if (map.TryGetValue("latitude", out var latObj) && latObj is double latD) lat = latD;
                        if (map.TryGetValue("longitude", out var lonObj) && lonObj is double lonD) lon = lonD;

                        loc.GpsRaw = new GpsRawLocation
                        {
                            UtcTime = utcTime,
                            Altitude = altitude,
                            Latitude = lat,
                            Longitude = lon,
                        };
                    }
                    break;

                case MMModemLocationSource.GpsNmea:
                    if (value is string nmea)
                    {
                        var lines = nmea.Split('\n')
                            .Select(l => l.Trim())
                            .Where(l => l.Contains('*'))
                            .ToArray();
                        loc.GpsNmea = new GpsNmeaLocation { NmeaSentences = lines };
                    }
                    break;

                case MMModemLocationSource.CdmaBs:
                    if (value is IDictionary<string, object> cdmaMap)
                    {
                        double lat = 0, lon = 0;
                        if (cdmaMap.TryGetValue("latitude", out var latObj) && latObj is double latD) lat = latD;
                        if (cdmaMap.TryGetValue("longitude", out var lonObj) && lonObj is double lonD) lon = lonD;
                        loc.CdmaBs = new CdmaBsLocation { Latitude = lat, Longitude = lon };
                    }
                    break;
            }
        }

        return loc;
    }
}
