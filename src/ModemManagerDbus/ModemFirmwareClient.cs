using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemFirmwareClient
{
    private readonly IModemFirmware _proxy;

    public ModemFirmwareClient(Connection connection, ObjectPath modemObjectPath)
    {
        _proxy = connection.CreateProxy<IModemFirmware>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public Task SelectAsync(string uniqueId) => _proxy.SelectAsync(uniqueId);

    public async Task<IReadOnlyList<FirmwareProperty>> ListAsync(CancellationToken cancellationToken = default)
    {
        var (selected, installed) = await _proxy.ListAsync().WaitAsync(cancellationToken);
        var list = new List<FirmwareProperty>();
        foreach (var dict in installed)
        {
            var fp = new FirmwareProperty();
            if (dict.TryGetValue("image-type", out var imgObj) && DbusConvert.TryToUInt32(imgObj, out var imgType))
                fp.ImageType = (MMFirmwareImageType)imgType;
            if (dict.TryGetValue("unique-id", out var uidObj) && DbusConvert.TryToString(uidObj, out var uid))
            {
                fp.UniqueId = uid;
                fp.Selected = uid == selected;
            }
            if (dict.TryGetValue("gobi-pri-version", out var priVObj) && DbusConvert.TryToString(priVObj, out var priV)) fp.GobiPriVersion = priV;
            if (dict.TryGetValue("gobi-pri-info", out var priIObj) && DbusConvert.TryToString(priIObj, out var priI)) fp.GobiPriInfo = priI;
            if (dict.TryGetValue("gobi-boot-version", out var bootObj) && DbusConvert.TryToString(bootObj, out var boot)) fp.GobiBootVersion = boot;
            if (dict.TryGetValue("gobi-pri-unique-id", out var priUidObj) && DbusConvert.TryToString(priUidObj, out var priUid)) fp.GobiPriUniqueId = priUid;
            if (dict.TryGetValue("gobi-modem-unique-id", out var mUidObj) && DbusConvert.TryToString(mUidObj, out var mUid)) fp.GobiModemUniqueId = mUid;
            list.Add(fp);
        }
        return list;
    }

    public async Task<UpdateSettingsProperty> GetUpdateSettingsAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _proxy.GetAsync<(uint updateMethodsBitmask, IDictionary<string, object> settings)>("UpdateSettings").WaitAsync(cancellationToken);
        var prop = new UpdateSettingsProperty
        {
            UpdateMethods = MMModemFirmwareUpdateMethodExt.BitmaskToSlice(raw.updateMethodsBitmask),
        };
        if (raw.settings.TryGetValue("device-ids", out var didObj) && didObj is string[] dids) prop.DeviceIds = dids;
        if (raw.settings.TryGetValue("version", out var verObj) && DbusConvert.TryToString(verObj, out var ver)) prop.Version = ver;
        if (raw.settings.TryGetValue("fastboot-at", out var fbObj) && DbusConvert.TryToString(fbObj, out var fb)) prop.FastbootAt = fb;
        return prop;
    }
}
