using Tmds.DBus;

namespace AvahiDbus;

public static class AvahiConstants
{
    public const string AvahiService = "org.freedesktop.Avahi";
    public static readonly ObjectPath AvahiPath = new("/");

    public const string AvahiServerInterface = "org.freedesktop.Avahi.Server";
    public const string AvahiEntryGroupInterface = "org.freedesktop.Avahi.EntryGroup";

    /// <summary>Publish on every interface avahi is configured to use (AVAHI_IF_UNSPEC).</summary>
    /// <remarks>
    /// This is not as broad as it looks: which interfaces avahi will actually touch is
    /// constrained by <c>allow-interfaces</c> in <c>avahi-daemon.conf</c>. Prefer keeping
    /// that restriction in the daemon config and passing AVAHI_IF_UNSPEC here — selecting
    /// interfaces in code as well would duplicate the policy in two places and let the two
    /// drift apart.
    /// </remarks>
    public const int InterfaceUnspecified = -1;

    /// <summary>Publish over both IPv4 and IPv6 (AVAHI_PROTO_UNSPEC).</summary>
    public const int ProtocolUnspecified = -1;

    /// <summary>Use the daemon's own domain (empty string means "local").</summary>
    public const string DefaultDomain = "";

    /// <summary>Use the daemon's own host name rather than overriding it.</summary>
    public const string DefaultHost = "";

    public const uint NoFlags = 0;

    // AvahiServerState
    public const int ServerRunning = 2;

    // AvahiEntryGroupState
    public const int EntryGroupUncommited = 0;
    public const int EntryGroupRegistering = 1;
    public const int EntryGroupEstablished = 2;
    public const int EntryGroupCollision = 3;
    public const int EntryGroupFailure = 4;
}
