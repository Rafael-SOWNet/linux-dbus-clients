using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemMessagingClient
{
    private readonly Connection _connection;
    private readonly IModemMessaging _proxy;

    public ModemMessagingClient(Connection connection, ObjectPath modemObjectPath)
    {
        _connection = connection;
        _proxy = connection.CreateProxy<IModemMessaging>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public async Task<IReadOnlyList<SmsClient>> ListAsync(CancellationToken cancellationToken = default)
    {
        var paths = await _proxy.ListAsync().WaitAsync(cancellationToken);
        return paths.Select(p => new SmsClient(_connection, p)).ToArray();
    }

    public Task DeleteAsync(SmsClient sms, CancellationToken cancellationToken = default)
        => _proxy.DeleteAsync(sms.ObjectPath).WaitAsync(cancellationToken);

    public async Task<SmsClient> CreateSmsAsync(string number, string text, IDictionary<string, object>? optional = null, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, object>
        {
            ["number"] = number,
            ["text"] = text,
        };
        if (optional != null)
        {
            foreach (var kvp in optional)
                dict[kvp.Key] = kvp.Value;
        }
        var path = await _proxy.CreateAsync(dict).WaitAsync(cancellationToken);
        return new SmsClient(_connection, path);
    }

    public async Task<SmsClient> CreateMmsAsync(string number, byte[] data, IDictionary<string, object>? optional = null, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, object>
        {
            ["number"] = number,
            ["data"] = data,
        };
        if (optional != null)
        {
            foreach (var kvp in optional)
                dict[kvp.Key] = kvp.Value;
        }
        var path = await _proxy.CreateAsync(dict).WaitAsync(cancellationToken);
        return new SmsClient(_connection, path);
    }

    public async Task<IReadOnlyList<SmsClient>> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        var paths = await _proxy.GetAsync<ObjectPath[]>("Messages").WaitAsync(cancellationToken);
        return paths.Select(p => new SmsClient(_connection, p)).ToArray();
    }

    public async Task<IReadOnlyList<MMSmsStorage>> GetSupportedStoragesAsync(CancellationToken cancellationToken = default)
    {
        var vals = await _proxy.GetAsync<uint[]>("SupportedStorages").WaitAsync(cancellationToken);
        return vals.Select(v => (MMSmsStorage)v).ToArray();
    }

    public async Task<MMSmsStorage> GetDefaultStorageAsync(CancellationToken cancellationToken = default)
        => (MMSmsStorage)await _proxy.GetAsync<uint>("DefaultStorage").WaitAsync(cancellationToken);

    public Task<IDisposable> WatchAddedAsync(Action<(SmsClient sms, bool received)> handler, Action<Exception>? onError = null)
        => _proxy.WatchAddedAsync(tuple => handler((new SmsClient(_connection, tuple.path), tuple.received)), onError);

    public Task<IDisposable> WatchDeletedAsync(Action<ObjectPath> handler, Action<Exception>? onError = null)
        => _proxy.WatchDeletedAsync(handler, onError);
}
