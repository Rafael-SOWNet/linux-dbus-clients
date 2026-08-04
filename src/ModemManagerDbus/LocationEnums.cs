using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMModemLocationSource : uint
{
    None = 0,
    _3gppLacCi = 1 << 0,
    GpsRaw = 1 << 1,
    GpsNmea = 1 << 2,
    CdmaBs = 1 << 3,
    GpsUnmanaged = 1 << 4,
    AgpsMsa = 1 << 5,
    AgpsMsb = 1 << 6,
}

public static class MMModemLocationSourceExt
{
    private static readonly MMModemLocationSource[] All =
    [
        MMModemLocationSource._3gppLacCi,
        MMModemLocationSource.GpsRaw,
        MMModemLocationSource.GpsNmea,
        MMModemLocationSource.CdmaBs,
        MMModemLocationSource.GpsUnmanaged,
        MMModemLocationSource.AgpsMsa,
        MMModemLocationSource.AgpsMsb,
    ];

    public static IReadOnlyList<MMModemLocationSource> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMModemLocationSource>();
        var result = new List<MMModemLocationSource>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if ((bitmask & (1u << idx)) != 0)
                result.Add(All[idx]);
        }
        return result;
    }

    public static uint SliceToBitmask(IEnumerable<MMModemLocationSource> sources)
    {
        uint bitmask = 0;
        var arr = sources.ToArray();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if (arr.Contains(All[idx]))
                bitmask |= (1u << idx);
        }
        return bitmask;
    }
}

public enum MMModemLocationAssistanceDataType : uint
{
    None = 0,
    Xtra = 1 << 0,
}

public static class MMModemLocationAssistanceDataTypeExt
{
    private static readonly MMModemLocationAssistanceDataType[] All = [MMModemLocationAssistanceDataType.Xtra];

    public static IReadOnlyList<MMModemLocationAssistanceDataType> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMModemLocationAssistanceDataType>();
        var result = new List<MMModemLocationAssistanceDataType>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if ((bitmask & (1u << idx)) != 0)
                result.Add(All[idx]);
        }
        return result;
    }
}

public enum MMModemMode : uint
{
    None = 0,
    Cs = 1 << 0,
    _2g = 1 << 1,
    _3g = 1 << 2,
    _4g = 1 << 3,
    Any = 0xFFFFFFFF,
}

public static class MMModemModeExt
{
    private static readonly MMModemMode[] All =
    [
        MMModemMode.Cs,
        MMModemMode._2g,
        MMModemMode._3g,
        MMModemMode._4g,
    ];

    public static IReadOnlyList<MMModemMode> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMModemMode>();

        var result = new List<MMModemMode>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if ((bitmask & (1u << idx)) != 0)
                result.Add(All[idx]);
        }
        return result;
    }

    public static uint SliceToBitmask(IEnumerable<MMModemMode> modes)
    {
        uint bitmask = 0;
        var arr = modes.ToArray();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if (arr.Contains(All[idx]))
                bitmask |= (1u << idx);
        }
        return bitmask;
    }
}

public enum MMModemBand : uint
{
    Unknown = 0,
    Any = 256,
}

public enum MMModemPortType : uint
{
    Unknown = 1,
    Net = 2,
    At = 3,
    Qcdm = 4,
    Gps = 5,
    Qmi = 6,
    Mbim = 7,
    Audio = 8,
}

public enum MMBearerIpFamily : uint
{
    None = 0,
    Ipv4 = 1 << 0,
    Ipv6 = 1 << 1,
    Ipv4v6 = 1 << 2,
    Any = 0xFFFFFFFF,
}

public static class MMBearerIpFamilyExt
{
    private static readonly MMBearerIpFamily[] All = [ MMBearerIpFamily.Ipv4, MMBearerIpFamily.Ipv6, MMBearerIpFamily.Ipv4v6 ];

    public static IReadOnlyList<MMBearerIpFamily> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMBearerIpFamily>();
        var result = new List<MMBearerIpFamily>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if ((bitmask & (1u << idx)) != 0)
                result.Add(All[idx]);
        }
        return result;
    }
}

public enum MMBearerIpMethod : uint
{
    Unknown = 0,
    Ppp = 1,
    Static = 2,
    Dhcp = 3,
}

public enum MMBearerAllowedAuth : uint
{
    Unknown = 0,
    None = 1 << 0,
    Pap = 1 << 1,
    Chap = 1 << 2,
    Mschap = 1 << 3,
    Mschapv2 = 1 << 4,
    Eap = 1 << 5,
}

public enum MMBearerType : uint
{
    Unknown = 0,
    Default = 1,
    DefaultAttach = 2,
    Dedicated = 3,
}

public enum MMModemCdmaRmProtocol : uint
{
    Unknown = 0,
    Async = 1,
    PacketRelay = 2,
    PacketNetworkPpp = 3,
    PacketNetworkSlip = 4,
    StuIii = 5,
}
