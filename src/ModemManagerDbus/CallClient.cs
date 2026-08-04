using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class CallClient
{
    private readonly ICall _proxy;
    private readonly ObjectPath _path;

    public CallClient(Connection connection, ObjectPath objectPath)
    {
        _path = objectPath;
        _proxy = connection.CreateProxy<ICall>(MmConstants.ModemManagerInterface, objectPath);
    }

    public ObjectPath ObjectPath => _path;

    public Task StartAsync() => _proxy.StartAsync();
    public Task AcceptAsync() => _proxy.AcceptAsync();
    public Task DeflectAsync(string number) => _proxy.DeflectAsync(number);
    public Task JoinMultipartyAsync() => _proxy.JoinMultipartyAsync();
    public Task LeaveMultipartyAsync() => _proxy.LeaveMultipartyAsync();
    public Task HangupAsync() => _proxy.HangupAsync();
    public Task SendDtmfAsync(string dtmf) => _proxy.SendDtmfAsync(dtmf);

    public async Task<MMCallState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<int>("State").WaitAsync(cancellationToken);
        return (MMCallState)val;
    }

    public async Task<MMCallStateReason> GetStateReasonAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<int>("StateReason").WaitAsync(cancellationToken);
        return (MMCallStateReason)val;
    }

    public async Task<MMCallDirection> GetDirectionAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<int>("Direction").WaitAsync(cancellationToken);
        return (MMCallDirection)val;
    }

    public Task<string> GetNumberAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Number").WaitAsync(cancellationToken);

    public Task<bool> GetMultipartyAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<bool>("Multiparty").WaitAsync(cancellationToken);

    public Task<string> GetAudioPortAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("AudioPort").WaitAsync(cancellationToken);

    public async Task<AudioFormat> GetAudioFormatAsync(CancellationToken cancellationToken = default)
    {
        var map = await _proxy.GetAsync<IDictionary<string, object>>("AudioFormat").WaitAsync(cancellationToken);

        var encoding = map.TryGetValue("encoding", out var encObj) && DbusConvert.TryToString(encObj, out var enc) ? enc : string.Empty;
        var resolution = map.TryGetValue("resolution", out var resObj) && DbusConvert.TryToString(resObj, out var res) ? res : string.Empty;
        var rate = map.TryGetValue("rate", out var rateObj) && DbusConvert.TryToUInt32(rateObj, out var rateValue) ? rateValue : 0;

        return new AudioFormat(encoding, resolution, rate);
    }
}
