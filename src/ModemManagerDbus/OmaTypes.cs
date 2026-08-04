using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMOmaFeature : uint
{
    None = 0,
    DeviceProvisioning = 1 << 0,
    PrlUpdate = 1 << 1,
    HandsFreeActivation = 1 << 2,
}

public static class MMOmaFeatureExt
{
    private static readonly MMOmaFeature[] All = [MMOmaFeature.DeviceProvisioning, MMOmaFeature.PrlUpdate, MMOmaFeature.HandsFreeActivation];

    public static IReadOnlyList<MMOmaFeature> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMOmaFeature>();
        var result = new List<MMOmaFeature>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if ((bitmask & (1u << idx)) != 0)
                result.Add(All[idx]);
        }
        return result;
    }

    public static uint SliceToBitmask(IEnumerable<MMOmaFeature> features)
    {
        uint bitmask = 0;
        var arr = features.ToArray();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if (arr.Contains(All[idx]))
                bitmask |= (1u << idx);
        }
        return bitmask;
    }
}

public enum MMOmaSessionType : uint
{
    Unknown = 0,
    ClientInitiatedDeviceConfigure = 10,
    ClientInitiatedPrlUpdate = 11,
    ClientInitiatedHandsFreeActivation = 12,
    NetworkInitiatedDeviceConfigure = 20,
    NetworkInitiatedPrlUpdate = 21,
    DeviceInitiatedPrlUpdate = 30,
    DeviceInitiatedHandsFreeActivation = 31,
}

public enum MMOmaSessionState : int
{
    Failed = -1,
    Unknown = 0,
    Started = 1,
    Retrying = 2,
    Connecting = 3,
    Connected = 4,
    Authenticated = 5,
    MdnDownloaded = 10,
    MsidDownloaded = 11,
    PrlDownloaded = 12,
    MipProfileDownloaded = 13,
    Completed = 20,
}

public enum MMOmaSessionStateFailedReason : uint
{
    Unknown = 0,
    NetworkUnavailable = 1,
    ServerUnavailable = 2,
    AuthenticationFailed = 3,
    MaxRetryExceeded = 4,
    SessionCancelled = 5,
}

public readonly record struct ModemOmaInitiatedSession(MMOmaSessionType SessionType, uint SessionId);
