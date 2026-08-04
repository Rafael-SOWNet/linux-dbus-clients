using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemTimeClient
{
    private readonly IModemTime _proxy;

    public ModemTimeClient(Connection connection, ObjectPath modemObjectPath)
    {
        _proxy = connection.CreateProxy<IModemTime>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public async Task<DateTimeOffset> GetNetworkTimeAsync(CancellationToken cancellationToken = default)
    {
        var s = await _proxy.GetNetworkTimeAsync().WaitAsync(cancellationToken);
        return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture);
    }

    public async Task<ModemTimeZone> GetNetworkTimezoneAsync(CancellationToken cancellationToken = default)
    {
        var map = await _proxy.GetAsync<IDictionary<string, object>>("NetworkTimezone").WaitAsync(cancellationToken);
        var tz = new ModemTimeZone();
        if (map.TryGetValue("offset", out var offObj) && offObj is int off) tz.Offset = off;
        if (map.TryGetValue("dst-offset", out var dstObj) && dstObj is int dst) tz.DstOffset = dst;
        if (map.TryGetValue("leap-seconds", out var leapObj) && leapObj is int leap) tz.LeapSeconds = leap;
        return tz;
    }

    public Task<IDisposable> WatchNetworkTimeChangedAsync(Action<DateTimeOffset> handler, Action<Exception>? onError = null)
        => _proxy.WatchNetworkTimeChangedAsync(s => handler(DateTimeOffset.Parse(s, CultureInfo.InvariantCulture)), onError);
}
