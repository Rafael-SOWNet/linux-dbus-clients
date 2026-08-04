using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMFirmwareImageType : uint
{
    Unknown = 0,
    Generic = 1,
    Gobi = 2,
}

public enum MMModemFirmwareUpdateMethod : uint
{
    None = 0,
    Fastboot = 1 << 0,
    QmiPdc = 1 << 1,
}

public static class MMModemFirmwareUpdateMethodExt
{
    private static readonly MMModemFirmwareUpdateMethod[] All = [MMModemFirmwareUpdateMethod.Fastboot, MMModemFirmwareUpdateMethod.QmiPdc];

    public static IReadOnlyList<MMModemFirmwareUpdateMethod> BitmaskToSlice(uint bitmask)
    {
        if (bitmask == 0) return Array.Empty<MMModemFirmwareUpdateMethod>();
        var result = new List<MMModemFirmwareUpdateMethod>();
        for (var idx = 0; idx < All.Length; idx++)
        {
            if ((bitmask & (1u << idx)) != 0)
                result.Add(All[idx]);
        }
        return result;
    }
}

public sealed class FirmwareProperty
{
    public MMFirmwareImageType ImageType { get; set; }
    public string UniqueId { get; set; } = string.Empty;
    public string GobiPriVersion { get; set; } = string.Empty;
    public string GobiPriInfo { get; set; } = string.Empty;
    public string GobiBootVersion { get; set; } = string.Empty;
    public string GobiPriUniqueId { get; set; } = string.Empty;
    public string GobiModemUniqueId { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public sealed class UpdateSettingsProperty
{
    public IReadOnlyList<MMModemFirmwareUpdateMethod> UpdateMethods { get; set; } = Array.Empty<MMModemFirmwareUpdateMethod>();
    public IReadOnlyList<string> DeviceIds { get; set; } = Array.Empty<string>();
    public string Version { get; set; } = string.Empty;
    public string FastbootAt { get; set; } = string.Empty;
}
