using Tmds.DBus;

namespace AvahiDbus.Dbus;

[DBusInterface(AvahiConstants.AvahiServerInterface)]
public interface IAvahiServerProxy : IDBusObject
{
    Task<string> GetHostNameAsync();
    Task<string> GetHostNameFqdnAsync();
    Task<int> GetStateAsync();
    Task<ObjectPath> EntryGroupNewAsync();
}

[DBusInterface(AvahiConstants.AvahiEntryGroupInterface)]
public interface IAvahiEntryGroupProxy : IDBusObject
{
    /// <param name="txt">
    /// DNS-SD TXT records as raw bytes (D-Bus signature <c>aay</c>). Each entry is one
    /// "key=value" record; avahi does not interpret them, so they must already be encoded.
    /// </param>
    Task AddServiceAsync(
        int @interface,
        int protocol,
        uint flags,
        string name,
        string type,
        string domain,
        string host,
        ushort port,
        byte[][] txt);

    Task CommitAsync();
    Task ResetAsync();
    Task FreeAsync();
    Task<int> GetStateAsync();
}
