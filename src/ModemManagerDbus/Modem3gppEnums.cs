using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMModem3gppRegistrationState : uint
{
    Idle = 0,
    Home = 1,
    Searching = 2,
    Denied = 3,
    Unknown = 4,
    Roaming = 5,
    HomeSmsOnly = 6,
    RoamingSmsOnly = 7,
    EmergencyOnly = 8,
    HomeCsfbNotPreferred = 9,
    RoamingCsfbNotPreferred = 10,
}

public enum MMModem3gppFacility : uint
{
    None = 0,
    Sim = 1 << 0,
    FixedDialing = 1 << 1,
    PhSim = 1 << 2,
    PhFsim = 1 << 3,
    NetPers = 1 << 4,
    NetSubPers = 1 << 5,
    ProviderPers = 1 << 6,
    CorpPers = 1 << 7,
}

public static class MMModem3gppFacilityExt
{
    private static readonly MMModem3gppFacility[] All =
    [
        MMModem3gppFacility.Sim,
        MMModem3gppFacility.FixedDialing,
        MMModem3gppFacility.PhSim,
        MMModem3gppFacility.NetPers,
        MMModem3gppFacility.NetSubPers,
        MMModem3gppFacility.ProviderPers,
        MMModem3gppFacility.CorpPers,
    ];

    public static IReadOnlyList<MMModem3gppFacility> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMModem3gppFacility>();
        var result = new List<MMModem3gppFacility>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if ((bitmask & (1u << idx)) != 0)
                result.Add(All[idx]);
        }
        return result;
    }
}

public enum MMModem3gppNetworkAvailability : uint
{
    Unknown = 0,
    Available = 1,
    Current = 2,
    Forbidden = 3,
}

public enum MMModem3gppEpsUeModeOperation : uint
{
    Unknown = 0,
    Ps1 = 1,
    Ps2 = 2,
    Csps1 = 3,
    Csps2 = 4,
}
