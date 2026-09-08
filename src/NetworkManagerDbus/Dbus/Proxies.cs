using Tmds.DBus;

namespace NetworkManagerDbus.Dbus;

[DBusInterface(NmConstants.NetworkManagerInterface)]
public interface INetworkManagerProxy : IDBusObject
{
    Task EnableAsync(bool enable);
    Task<ObjectPath[]> GetDevicesAsync();
    Task<ObjectPath> ActivateConnectionAsync(ObjectPath connection, ObjectPath device, ObjectPath specificObject);
    Task<T> GetAsync<T>(string prop);
    Task SetAsync(string prop, object value);
}

[DBusInterface(NmConstants.NetworkManagerDeviceInterface)]
public interface INetworkManagerDeviceProxy : IDBusObject
{
    Task ReapplyAsync(IDictionary<string, IDictionary<string, object>> connection, ulong versionId, uint flags);
    Task<T> GetAsync<T>(string prop);
}

[DBusInterface(NmConstants.NetworkManagerWirelessDeviceInterface)]
public interface INetworkManagerWirelessDeviceProxy : IDBusObject
{
    Task RequestScanAsync(IDictionary<string, object> options);
    Task<T> GetAsync<T>(string prop);
}

[DBusInterface(NmConstants.NetworkManagerAccessPointInterface)]
public interface INetworkManagerAccessPointProxy : IDBusObject
{
    Task<T> GetAsync<T>(string prop);
}

[DBusInterface(NmConstants.NetworkManagerIp4ConfigInterface)]
public interface INetworkManagerIp4ConfigProxy : IDBusObject
{
    Task<T> GetAsync<T>(string prop);
}

[DBusInterface(NmConstants.NetworkManagerIp6ConfigInterface)]
public interface INetworkManagerIp6ConfigProxy : IDBusObject
{
    Task<T> GetAsync<T>(string prop);
}

[DBusInterface(NmConstants.NetworkManagerSettingsInterface)]
public interface INetworkManagerSettingsProxy : IDBusObject
{
    Task<ObjectPath[]> ListConnectionsAsync();
    Task<ObjectPath> AddConnectionAsync(IDictionary<string, IDictionary<string, object>> connection);
}

[DBusInterface(NmConstants.NetworkManagerConnectionInterface)]
public interface INetworkManagerConnectionProxy : IDBusObject
{
    Task<IDictionary<string, IDictionary<string, object>>> GetSettingsAsync();

    /// <summary>
    /// Secrets for a single setting group, e.g. "802-11-wireless-security".
    ///
    /// <see cref="GetSettingsAsync"/> deliberately omits secrets, so on its own it cannot be used
    /// to round-trip a connection: <see cref="UpdateAsync"/> replaces the connection wholesale, so
    /// writing back settings that were read without secrets silently deletes them.
    /// </summary>
    Task<IDictionary<string, IDictionary<string, object>>> GetSecretsAsync(string settingName);

    Task UpdateAsync(IDictionary<string, IDictionary<string, object>> properties);
    Task DeleteAsync();
}
