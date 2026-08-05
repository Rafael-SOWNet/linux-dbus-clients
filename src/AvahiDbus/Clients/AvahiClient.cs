using System.Text;
using AvahiDbus.Dbus;
using Tmds.DBus;

namespace AvahiDbus.Clients;

/// <summary>
/// Thin wrapper over avahi's D-Bus API for publishing a DNS-SD service at runtime.
/// </summary>
/// <remarks>
/// Runtime registration is worth the trouble over a static file in <c>/etc/avahi/services</c>
/// whenever the interesting TXT fields - build version, hardware serial, which port the
/// caller should talk to - are only known to the running process. A static file can only
/// carry values that were fixed when the image was built.
///
/// Creating an entry group is a privileged operation: avahi's D-Bus policy (the drop-in
/// files under <c>/etc/dbus-1/system.d/</c>) decides which users may do it, and typically
/// only root is allowed by default. Grant your service account explicitly. A denial
/// surfaces here as a <see cref="DBusException"/> with "Access denied", not as a silent
/// no-op, so it is safe to let it propagate.
/// </remarks>
public sealed class AvahiClient : IAsyncDisposable
{
    private readonly Connection _connection;
    private readonly IAvahiServerProxy _server;
    private IAvahiEntryGroupProxy? _entryGroup;

    private AvahiClient(Connection connection)
    {
        _connection = connection;
        _server = _connection.CreateProxy<IAvahiServerProxy>(AvahiConstants.AvahiService, AvahiConstants.AvahiPath);
    }

    /// <summary>Connects to the system bus and returns a client owning that connection.</summary>
    /// <remarks>
    /// The connection is disposed if connecting fails. Until it reaches the client nothing else
    /// can release it — the caller receives an exception rather than an object — so a connection
    /// leaked here is unreachable for the rest of the process's life.
    ///
    /// That matters most precisely when this is most likely to fail: a bus refuses a UID that has
    /// reached <c>max_connections_per_user</c> (256 by default), so a process near the ceiling
    /// throws on every further attempt, and each of those would otherwise leak one more and push
    /// it further past the limit.
    /// </remarks>
    public static async Task<AvahiClient> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new Connection(Address.System);
        try
        {
            await connection.ConnectAsync().WaitAsync(cancellationToken);
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        return new AvahiClient(connection);
    }

    /// <summary>The host name avahi is currently publishing, without the .local suffix.</summary>
    /// <remarks>
    /// Worth reading rather than assuming it matches <c>hostname</c>: when another device on
    /// the link already owns the name, avahi resolves the conflict by publishing a suffixed
    /// variant instead (<c>mydevice</c> becomes <c>mydevice-2</c>). This is not hypothetical -
    /// it happens the moment two boards ship with the same default hostname. Anything that
    /// reports the device's discoverable name back to a user must use this value, not the
    /// system hostname, or it will tell them to connect to a name that resolves elsewhere.
    /// </remarks>
    public Task<string> GetPublishedHostNameAsync(CancellationToken cancellationToken = default)
        => _server.GetHostNameAsync().WaitAsync(cancellationToken);

    public Task<int> GetServerStateAsync(CancellationToken cancellationToken = default)
        => _server.GetStateAsync().WaitAsync(cancellationToken);

    /// <summary>
    /// State of the entry group this client published, or null if it has published nothing.
    /// </summary>
    /// <remarks>
    /// This is the only reliable way to notice that a published record has gone away. When
    /// avahi-daemon restarts it drops every entry group it was holding, but the host name is
    /// unchanged across that restart - so a caller that only watches the host name concludes
    /// nothing happened and never republishes. The failure mode is quiet and easy to miss:
    /// a service published over D-Bus disappears from the network across an
    /// <c>systemctl restart avahi-daemon</c> and never comes back, while services declared in
    /// a static /etc/avahi/services file reappear immediately. Poll this and republish when it
    /// stops reporting Established.
    ///
    /// In practice the daemon restart is usually caught by this call *throwing* rather than by
    /// the state it returns: the entry group object dies with the daemon, so the proxy call
    /// fails with "UnknownObject: Method GetState ... doesn't exist" before any state comes
    /// back. Both outcomes drive the same recovery, so this reports the failure rather than
    /// swallowing it - a null or non-established state covers the remaining case, where the
    /// group is merely collided or reset while the daemon is still up.
    /// </remarks>
    public async Task<int?> GetEntryGroupStateAsync(CancellationToken cancellationToken = default)
    {
        if (_entryGroup is null)
            return null;

        return await _entryGroup.GetStateAsync().WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Publishes a DNS-SD service. Replaces any service this client published earlier.
    /// </summary>
    /// <param name="serviceName">
    /// Instance name shown to browsers. Avahi appends " #2" on collision rather than failing.
    /// </param>
    /// <param name="serviceType">Service type, e.g. <c>_http._tcp</c>.</param>
    /// <param name="txtRecords">Key/value pairs published as TXT records.</param>
    public async Task PublishServiceAsync(
        string serviceName,
        string serviceType,
        ushort port,
        IReadOnlyDictionary<string, string> txtRecords,
        CancellationToken cancellationToken = default)
    {
        _entryGroup ??= _connection.CreateProxy<IAvahiEntryGroupProxy>(
            AvahiConstants.AvahiService,
            await _server.EntryGroupNewAsync().WaitAsync(cancellationToken));

        // Reset rather than Free: the group is reusable, and reusing it means a republish
        // (e.g. after a version change) does not leak entry groups in the daemon.
        await _entryGroup.ResetAsync().WaitAsync(cancellationToken);

        await _entryGroup.AddServiceAsync(
            AvahiConstants.InterfaceUnspecified,
            AvahiConstants.ProtocolUnspecified,
            AvahiConstants.NoFlags,
            serviceName,
            serviceType,
            AvahiConstants.DefaultDomain,
            AvahiConstants.DefaultHost,
            port,
            EncodeTxtRecords(txtRecords)).WaitAsync(cancellationToken);

        await _entryGroup.CommitAsync().WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Encodes TXT records as DNS-SD expects: one UTF-8 "key=value" byte string per record.
    /// </summary>
    /// <remarks>
    /// Records whose value is null or empty are skipped entirely rather than published as a
    /// bare "key=". A consumer cannot distinguish "key present but empty" from "key absent"
    /// in any useful way, and an empty field reads as real data when it is really a gap.
    /// </remarks>
    private static byte[][] EncodeTxtRecords(IReadOnlyDictionary<string, string> txtRecords)
        => txtRecords
            .Where(static kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(static kvp => Encoding.UTF8.GetBytes($"{kvp.Key}={kvp.Value}"))
            .ToArray();

    public async ValueTask DisposeAsync()
    {
        // Freeing the group withdraws the records immediately instead of leaving them to
        // time out, so a backend restart does not advertise a stale port for the TTL window.
        if (_entryGroup is not null)
        {
            try
            {
                await _entryGroup.FreeAsync();
            }
            catch
            {
                // The daemon may already be gone during shutdown; nothing useful to do.
            }
        }

        _connection.Dispose();
    }
}
