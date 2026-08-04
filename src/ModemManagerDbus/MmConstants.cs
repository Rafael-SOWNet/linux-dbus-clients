using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public static class MmConstants
{
    public const string ModemManagerInterface = "org.freedesktop.ModemManager1";
    public static readonly ObjectPath ModemManagerObjectPath = new ObjectPath("/org/freedesktop/ModemManager1");

    public const string ModemInterface = ModemManagerInterface + ".Modem";
    public const string BearerInterface = ModemManagerInterface + ".Bearer";
    public const string SimInterface = ModemManagerInterface + ".Sim";
    public const string SmsInterface = ModemManagerInterface + ".Sms";
    public const string CallInterface = ModemManagerInterface + ".Call";

    public const string ObjectManagerInterface = "org.freedesktop.DBus.ObjectManager";
    public const string PropertiesInterface = "org.freedesktop.DBus.Properties";

    public static class Logging
    {
        public const string Error = "ERR";
        public const string Warning = "WARN";
        public const string Debug = "DEBUG";
    }
}

public record struct Pair<TLeft, TRight>(TLeft Left, TRight Right);

public readonly record struct Mode(IReadOnlyList<MMModemMode> AllowedModes, MMModemMode PreferredMode);

public readonly record struct Port(string PortName, MMModemPortType PortType);

// ReportKernelEvent payload: D-Bus signature `a{sv}`.
[Dictionary]
public class EventProperties
{
    // For the DBus signature a{sv}, Tmds serializes [Dictionary] classes by field name (with dash/underscore normalization).
    // ModemManager expects keys: action, name, subsystem, uid.
    public string action = default!;
    public string name = default!;
    public string subsystem = default!;
    public string? uid = null;
}

// Modem.CreateBearer payload: D-Bus signature `a{sv}`.
[Dictionary]
public class BearerProperty
{
    // For the DBus signature a{sv}, keys must match ModemManager: apn, ip-type, allowed-auth, user, password, allow-roaming, rm-protocol, number.
    public string? apn = null;
    public MMBearerIpFamily? ip_type = null;
    public MMBearerAllowedAuth? allowed_auth = null;
    public string? user = null;
    public string? password = null;
    public bool? allow_roaming = null;
    public MMModemCdmaRmProtocol? rm_protocol = null;
    public string? number = null;
}
