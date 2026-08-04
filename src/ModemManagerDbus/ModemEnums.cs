using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMModemCapability : uint
{
    None = 0,
    Pots = 1 << 0,
    CdmaEvdo = 1 << 1,
    GsmUmts = 1 << 2,
    Lte = 1 << 3,
    LteAdvanced = 1 << 4,
    Iridium = 1 << 5,
    Any = 0xFFFFFFFF,
}

public static class MMModemCapabilityExt
{
    private static readonly MMModemCapability[] All =
    [
        MMModemCapability.Pots,
        MMModemCapability.CdmaEvdo,
        MMModemCapability.GsmUmts,
        MMModemCapability.Lte,
        MMModemCapability.LteAdvanced,
        MMModemCapability.Iridium,
    ];

    public static IReadOnlyList<MMModemCapability> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMModemCapability>();

        var result = new List<MMModemCapability>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            var flag = All[idx];
            if ((bitmask & (1u << idx)) != 0)
                result.Add(flag);
        }
        return result;
    }

    public static uint SliceToBitmask(IEnumerable<MMModemCapability> capabilities)
    {
        uint bitmask = 0;
        var arr = capabilities.ToArray();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if (arr.Contains(All[idx]))
                bitmask |= (1u << idx);
        }
        return bitmask;
    }
}

public enum MMModemLock : uint
{
    Unknown = 0,
    None = 1,
    SimPin = 2,
    SimPin2 = 3,
    SimPuk = 4,
    SimPuk2 = 5,
    PhSpPin = 6,
    PhSpPuk = 7,
    PhNetPin = 8,
    PhNetPuk = 9,
    PhSimPin = 10,
    PhCorpPin = 11,
    PhCorpPuk = 12,
    PhFsimPin = 13,
    PhFsimPuk = 14,
    PhNetsubPin = 15,
    PhNetsubPuk = 16,
}

public enum MMModemState : int
{
    Failed = -1,
    Unknown = 0,
    Initializing = 1,
    Locked = 2,
    Disabled = 3,
    Disabling = 4,
    Enabling = 5,
    Enabled = 6,
    Searching = 7,
    Registered = 8,
    Disconnecting = 9,
    Connecting = 10,
    Connected = 11,
}

public enum MMModemStateFailedReason : uint
{
    None = 0,
    Unknown = 1,
    SimMissing = 2,
    SimError = 3,
    UnknownCapabilities = 4,
    EsimWithoutProfiles = 5,
}

public enum MMModemStateChangeReason : uint
{
    Unknown = 0,
    UserRequested = 1,
    Suspend = 2,
    Failure = 3,
}

public enum MMModemPowerState : uint
{
    Unknown = 0,
    Off = 1,
    Low = 2,
    On = 3,
}

public enum MMModemAccessTechnology : uint
{
    Unknown = 0,
    Pots = 1 << 0,
    Gsm = 1 << 1,
    GsmCompact = 1 << 2,
    Gprs = 1 << 3,
    Edge = 1 << 4,
    Umts = 1 << 5,
    Hsdpa = 1 << 6,
    Hsupa = 1 << 7,
    Hspa = 1 << 8,
    HspaPlus = 1 << 9,
    _1xrtt = 1 << 10,
    Evdo0 = 1 << 11,
    Evdoa = 1 << 12,
    Evdob = 1 << 13,
    Lte = 1 << 14,
    Any = 0xFFFFFFFF,
}

public static class MMModemAccessTechnologyExt
{
    private static readonly MMModemAccessTechnology[] All =
    [
        MMModemAccessTechnology.Pots,
        MMModemAccessTechnology.Gsm,
        MMModemAccessTechnology.GsmCompact,
        MMModemAccessTechnology.Gprs,
        MMModemAccessTechnology.Edge,
        MMModemAccessTechnology.Umts,
        MMModemAccessTechnology.Hsdpa,
        MMModemAccessTechnology.Hsupa,
        MMModemAccessTechnology.Hspa,
        MMModemAccessTechnology.HspaPlus,
        MMModemAccessTechnology._1xrtt,
        MMModemAccessTechnology.Evdo0,
        MMModemAccessTechnology.Evdoa,
        MMModemAccessTechnology.Evdob,
        MMModemAccessTechnology.Lte,
    ];

    public static IReadOnlyList<MMModemAccessTechnology> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMModemAccessTechnology>();

        var result = new List<MMModemAccessTechnology>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if ((bitmask & (1u << idx)) != 0)
                result.Add(All[idx]);
        }
        return result;
    }
}
