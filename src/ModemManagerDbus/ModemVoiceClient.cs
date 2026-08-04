using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemVoiceClient
{
    private readonly Connection _connection;
    private readonly IModemVoice _proxy;

    public ModemVoiceClient(Connection connection, ObjectPath modemObjectPath)
    {
        _connection = connection;
        _proxy = connection.CreateProxy<IModemVoice>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public async Task<IReadOnlyList<CallClient>> ListCallsAsync(CancellationToken cancellationToken = default)
    {
        var paths = await _proxy.ListCallsAsync().WaitAsync(cancellationToken);
        return paths.Select(p => new CallClient(_connection, p)).ToArray();
    }

    public Task DeleteCallAsync(ObjectPath callPath, CancellationToken cancellationToken = default)
        => _proxy.DeleteCallAsync(callPath).WaitAsync(cancellationToken);

    public async Task<CallClient> CreateCallAsync(string number, IDictionary<string, object>? optional = null, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, object> { ["number"] = number };
        if (optional != null)
        {
            foreach (var kvp in optional)
                dict[kvp.Key] = kvp.Value;
        }
        var path = await _proxy.CreateCallAsync(dict).WaitAsync(cancellationToken);
        return new CallClient(_connection, path);
    }

    public Task HoldAndAcceptAsync() => _proxy.HoldAndAcceptAsync();
    public Task HangupAndAcceptAsync() => _proxy.HangupAndAcceptAsync();
    public Task HangupAllAsync() => _proxy.HangupAllAsync();
    public Task TransferAsync() => _proxy.TransferAsync();
    public Task CallWaitingSetupAsync(bool enable) => _proxy.CallWaitingSetupAsync(enable);
    public Task CallWaitingQueryAsync(bool status) => _proxy.CallWaitingQueryAsync(status);

    public async Task<IReadOnlyList<CallClient>> GetCallsAsync(CancellationToken cancellationToken = default)
    {
        var paths = await _proxy.GetAsync<ObjectPath[]>("Calls").WaitAsync(cancellationToken);
        return paths.Select(p => new CallClient(_connection, p)).ToArray();
    }

    public Task<bool> GetEmergencyOnlyAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<bool>("EmergencyOnly").WaitAsync(cancellationToken);

    public Task<IDisposable> WatchCallAddedAsync(Action<CallClient> handler, Action<Exception>? onError = null)
        => _proxy.WatchCallAddedAsync(p => handler(new CallClient(_connection, p)), onError);

    public Task<IDisposable> WatchCallDeletedAsync(Action<ObjectPath> handler, Action<Exception>? onError = null)
        => _proxy.WatchCallDeletedAsync(handler, onError);
}
