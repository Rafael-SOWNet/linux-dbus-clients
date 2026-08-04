using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMCallState : int
{
    Unknown = 0,
    Dialing = 1,
    RingingOut = 2,
    RingingIn = 3,
    Active = 4,
    Held = 5,
    Waiting = 6,
    Terminated = 7,
}

public enum MMCallStateReason : int
{
    Unknown = 0,
    OutgoingStarted = 1,
    IncomingNew = 2,
    Accepted = 3,
    Terminated = 4,
    RefusedOrBusy = 5,
    Error = 6,
    AudioSetupFailed = 7,
    Transferred = 8,
    Deflected = 9,
}

public enum MMCallDirection : int
{
    Unknown = 0,
    Incoming = 1,
    Outgoing = 2,
}

public static class DbusConvert
{
    public static bool TryToUInt32(object? value, out uint result)
    {
        result = 0;
        if (value is null) return false;
        try
        {
            result = Convert.ToUInt32(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryToUInt64(object? value, out ulong result)
    {
        result = 0;
        if (value is null) return false;
        try
        {
            result = Convert.ToUInt64(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryToBool(object? value, out bool result)
    {
        result = false;
        if (value is null) return false;
        try
        {
            result = Convert.ToBoolean(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryToString(object? value, out string result)
    {
        result = string.Empty;
        if (value is null) return false;
        if (value is string s)
        {
            result = s;
            return true;
        }
        try
        {
            result = Convert.ToString(value) ?? string.Empty;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
