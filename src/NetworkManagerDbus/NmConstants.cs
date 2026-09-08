using Tmds.DBus;

namespace NetworkManagerDbus;

public static class NmConstants
{
    public const uint DeviceTypeWifi = 2;

    public const string NetworkManagerService = "org.freedesktop.NetworkManager";
    public static readonly ObjectPath NetworkManagerPath = new("/org/freedesktop/NetworkManager");
    public static readonly ObjectPath NetworkManagerSettingsPath = new("/org/freedesktop/NetworkManager/Settings");

    public const string NetworkManagerInterface = "org.freedesktop.NetworkManager";
    public const string NetworkManagerSettingsInterface = "org.freedesktop.NetworkManager.Settings";
    public const string NetworkManagerConnectionInterface = "org.freedesktop.NetworkManager.Settings.Connection";

    /// <summary>
    /// An *activated* connection, as opposed to the stored profile on
    /// <see cref="NetworkManagerConnectionInterface"/>. Note the name is
    /// "Connection.Active", not "ActiveConnection".
    /// </summary>
    public const string NetworkManagerActiveConnectionInterface = "org.freedesktop.NetworkManager.Connection.Active";
    public const string NetworkManagerDeviceInterface = "org.freedesktop.NetworkManager.Device";
    public const string NetworkManagerWirelessDeviceInterface = "org.freedesktop.NetworkManager.Device.Wireless";
    public const string NetworkManagerAccessPointInterface = "org.freedesktop.NetworkManager.AccessPoint";
    public const string NetworkManagerIp4ConfigInterface = "org.freedesktop.NetworkManager.IP4Config";
    public const string NetworkManagerIp6ConfigInterface = "org.freedesktop.NetworkManager.IP6Config";
}
