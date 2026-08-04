using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemSimpleClient
{
    private readonly Connection _connection;
    private readonly IModemSimple _proxy;

    public ModemSimpleClient(Connection connection, ObjectPath modemObjectPath)
    {
        _connection = connection;
        _proxy = connection.CreateProxy<IModemSimple>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public async Task<BearerClient> ConnectAsync(SimpleProperties properties, CancellationToken cancellationToken = default)
    {
        var path = await _proxy.ConnectAsync(properties).WaitAsync(cancellationToken);
        return new BearerClient(_connection, path);
    }

    public Task DisconnectAsync(BearerClient bearer, CancellationToken cancellationToken = default)
        => _proxy.DisconnectAsync(bearer.ObjectPath).WaitAsync(cancellationToken);

    public async Task<SimpleStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var dict = await _proxy.GetStatusAsync().WaitAsync(cancellationToken);
        var status = new SimpleStatus();

        if (dict.TryGetValue("state", out var stateObj) && DbusConvert.TryToUInt32(stateObj, out var state))
            status.State = (MMModemState)(int)state;

        if (dict.TryGetValue("signal-quality", out var sigObj) && sigObj is object[] sigArr && sigArr.Length > 0 && DbusConvert.TryToUInt32(sigArr[0], out var sig))
            status.SignalQuality = sig;

        if (dict.TryGetValue("current-bands", out var bandsObj) && bandsObj is uint[] bands)
            status.CurrentBands = bands.Select(b => (MMModemBand)b).ToArray();

        if (dict.TryGetValue("access-technologies", out var techObj) && DbusConvert.TryToUInt32(techObj, out var tech))
            status.AccessTechnology = (MMModemAccessTechnology)tech;

        if (dict.TryGetValue("m3gpp-registration-state", out var regObj) && DbusConvert.TryToUInt32(regObj, out var reg))
            status.M3GppRegistrationState = (MMModem3gppRegistrationState)reg;
        if (dict.TryGetValue("m3gpp-operator-code", out var opCodeObj) && DbusConvert.TryToString(opCodeObj, out var opCode))
            status.M3GppOperatorCode = opCode;
        if (dict.TryGetValue("m3gpp-operator-name", out var opNameObj) && DbusConvert.TryToString(opNameObj, out var opName))
            status.M3GppOperatorName = opName;

        if (dict.TryGetValue("cdma-cdma1x-registration-state", out var cdma1xObj) && DbusConvert.TryToUInt32(cdma1xObj, out var cdma1x))
            status.CdmaCdma1xRegistrationState = (MMModemCdmaRegistrationState)cdma1x;
        if (dict.TryGetValue("cdma-evdo-registration-state", out var evdoObj) && DbusConvert.TryToUInt32(evdoObj, out var evdo))
            status.CdmaEvdoRegistrationState = (MMModemCdmaRegistrationState)evdo;
        if (dict.TryGetValue("cdma-sid", out var sidObj) && DbusConvert.TryToUInt32(sidObj, out var sid))
            status.CdmaSid = sid;
        if (dict.TryGetValue("cdma-nid", out var nidObj) && DbusConvert.TryToUInt32(nidObj, out var nid))
            status.CdmaNid = nid;

        return status;
    }
}
