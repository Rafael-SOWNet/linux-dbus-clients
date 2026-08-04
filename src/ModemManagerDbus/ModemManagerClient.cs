using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemManagerClient : IAsyncDisposable
{
    private readonly Connection _connection;
    private readonly IModemManager1 _manager;
    private readonly IObjectManager _objectManager;

    private ModemManagerClient(Connection connection)
    {
        _connection = connection;
        _manager = connection.CreateProxy<IModemManager1>(MmConstants.ModemManagerInterface, MmConstants.ModemManagerObjectPath);
        _objectManager = connection.CreateProxy<IObjectManager>(MmConstants.ModemManagerInterface, MmConstants.ModemManagerObjectPath);
    }

    public static async Task<ModemManagerClient> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new Connection(Address.System);
        await connection.ConnectAsync().WaitAsync(cancellationToken);
        return new ModemManagerClient(connection);
    }

    public ValueTask DisposeAsync()
    {
        _connection.Dispose();
        return ValueTask.CompletedTask;
    }

    public Task ScanDevicesAsync() => _manager.ScanDevicesAsync();

    // In the Go wrapper this is SetLogging(level MMLoggingLevel) where MMLoggingLevel is a string (ERR/WARN/DEBUG).
    public Task SetLoggingAsync(string level) => _manager.SetLoggingAsync(level);

    public Task ReportKernelEventAsync(EventProperties properties) => _manager.ReportKernelEventAsync(properties);

    public Task InhibitDeviceAsync(string uid, bool inhibit) => _manager.InhibitDeviceAsync(uid, inhibit);

    public Task<string> GetVersionAsync() => _manager.GetAsync<string>("Version");

    public async Task<IReadOnlyList<ModemClient>> GetModemsAsync(CancellationToken cancellationToken = default)
    {
        var managedObjects = await _objectManager.GetManagedObjectsAsync().WaitAsync(cancellationToken);

        var modems = new List<ModemClient>();

        foreach (var kvp in managedObjects)
        {
            var objectPath = kvp.Key;
            var interfaces = kvp.Value;

            if (interfaces != null && interfaces.ContainsKey(MmConstants.ModemInterface))
                modems.Add(new ModemClient(_connection, objectPath));
        }

        return modems;
    }

    public Task<IDisposable> WatchPropertiesChangedAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null)
        => _manager.WatchPropertiesAsync(handler, onError);
}
