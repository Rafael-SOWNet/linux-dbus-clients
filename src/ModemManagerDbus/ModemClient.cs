using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemClient
{
    private readonly Connection _connection;
    private readonly IModem _proxy;
    private readonly ObjectPath _path;

    public ModemClient(Connection connection, ObjectPath objectPath)
    {
        _connection = connection;
        _path = objectPath;
        _proxy = connection.CreateProxy<IModem>(MmConstants.ModemManagerInterface, objectPath);
    }

    public ObjectPath ObjectPath => _path;

    public Task EnableAsync() => _proxy.EnableAsync();

    public Task DisableAsync() => _proxy.DisableAsync();

    public Task<ObjectPath> CreateBearerAsync(BearerProperty properties)
        => _proxy.CreateBearerAsync(properties);

    public Task DeleteBearerAsync(ObjectPath bearer) => _proxy.DeleteBearerAsync(bearer);

    public Task ResetAsync() => _proxy.ResetAsync();

    public Task FactoryResetAsync(string code) => _proxy.FactoryResetAsync(code);

    public Task SetPowerStateAsync(MMModemPowerState state) => _proxy.SetPowerStateAsync((uint)state);

    public Task SetCurrentCapabilitiesAsync(IEnumerable<MMModemCapability> capabilities)
    {
        var bitmask = MMModemCapabilityExt.SliceToBitmask(capabilities);
        return _proxy.SetCurrentCapabilitiesAsync(bitmask);
    }

    public Task SetCurrentModesAsync(Mode mode)
    {
        var allowed = MMModemModeExt.SliceToBitmask(mode.AllowedModes);
        return _proxy.SetCurrentModesAsync((allowed, (uint)mode.PreferredMode));
    }

    public Task SetCurrentBandsAsync(IEnumerable<MMModemBand> bands)
    {
        var arr = bands.Select(b => (uint)b).ToArray();
        return _proxy.SetCurrentBandsAsync(arr);
    }

    public Task<string> CommandAsync(string cmd, uint timeout) => _proxy.CommandAsync(cmd, timeout);

    public async Task<SimClient> GetSimAsync(CancellationToken cancellationToken = default)
    {
        var simPath = await _proxy.GetAsync<ObjectPath>("Sim").WaitAsync(cancellationToken);
        return new SimClient(_connection, simPath);
    }

    public Modem3gppClient Get3gpp() => new(_connection, _path);

    public ModemSignalClient GetSignal() => new(_connection, _path);

    public ModemLocationClient GetLocation() => new(_connection, _path);

    public ModemSimpleClient GetSimple() => new(_connection, _path);

    public ModemMessagingClient GetMessaging() => new(_connection, _path);

    public ModemTimeClient GetTime() => new(_connection, _path);

    public ModemFirmwareClient GetFirmware() => new(_connection, _path);

    public ModemVoiceClient GetVoice() => new(_connection, _path);

    public UssdClient GetUssd() => new(_connection, _path);

    public ModemOmaClient GetOma() => new(_connection, _path);

    public ModemCdmaClient GetCdma() => new(_connection, _path);

    public async Task<IReadOnlyList<BearerClient>> GetBearersAsync(CancellationToken cancellationToken = default)
    {
        var bearerPaths = await _proxy.GetAsync<ObjectPath[]>("Bearers").WaitAsync(cancellationToken);
        return bearerPaths.Select(p => new BearerClient(_connection, p)).ToArray();
    }

    public async Task<IReadOnlyList<IReadOnlyList<MMModemCapability>>> GetSupportedCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var caps = await _proxy.GetAsync<uint[]>("SupportedCapabilities").WaitAsync(cancellationToken);
        return caps.Select(c => MMModemCapabilityExt.BitmaskToSlice(c)).ToArray();
    }

    public async Task<IReadOnlyList<MMModemCapability>> GetCurrentCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var caps = await _proxy.GetAsync<uint>("CurrentCapabilities").WaitAsync(cancellationToken);
        return MMModemCapabilityExt.BitmaskToSlice(caps);
    }

    public Task<uint> GetMaxBearersAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<uint>("MaxBearers").WaitAsync(cancellationToken);

    public Task<uint> GetMaxActiveBearersAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<uint>("MaxActiveBearers").WaitAsync(cancellationToken);

    public Task<string> GetManufacturerAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Manufacturer").WaitAsync(cancellationToken);

    public Task<string> GetModelAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Model").WaitAsync(cancellationToken);

    public Task<string> GetRevisionAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Revision").WaitAsync(cancellationToken);

    public Task<string> GetCarrierConfigurationAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("CarrierConfiguration").WaitAsync(cancellationToken);

    public Task<string> GetCarrierConfigurationRevisionAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("CarrierConfigurationRevision").WaitAsync(cancellationToken);

    public Task<string> GetHardwareRevisionAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("HardwareRevision").WaitAsync(cancellationToken);

    public Task<string> GetDeviceIdentifierAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("DeviceIdentifier").WaitAsync(cancellationToken);

    public Task<string> GetDeviceAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Device").WaitAsync(cancellationToken);

    public Task<IReadOnlyList<string>> GetDriversAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string[]>("Drivers").WaitAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<string>)t.Result, cancellationToken);

    public Task<string> GetPluginAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Plugin").WaitAsync(cancellationToken);

    public Task<string> GetPrimaryPortAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("PrimaryPort").WaitAsync(cancellationToken);

    public async Task<IReadOnlyList<Port>> GetPortsAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _proxy.GetAsync<(string portName, uint portType)[]>("Ports").WaitAsync(cancellationToken);
        return raw.Select(p => new Port(p.portName, (MMModemPortType)p.portType)).ToArray();
    }

    public Task<string> GetEquipmentIdentifierAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("EquipmentIdentifier").WaitAsync(cancellationToken);

    public async Task<MMModemLock> GetUnlockRequiredAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("UnlockRequired").WaitAsync(cancellationToken);
        return (MMModemLock)val;
    }

    public async Task<IReadOnlyList<Pair<MMModemLock, uint>>> GetUnlockRetriesAsync(CancellationToken cancellationToken = default)
    {
        var dict = await _proxy.GetAsync<IDictionary<uint, uint>>("UnlockRetries").WaitAsync(cancellationToken);
        return dict.Select(kvp => new Pair<MMModemLock, uint>((MMModemLock)kvp.Key, kvp.Value)).ToArray();
    }

    public async Task<MMModemState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<int>("State").WaitAsync(cancellationToken);
        return (MMModemState)val;
    }

    public async Task<MMModemStateFailedReason> GetStateFailedReasonAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("StateFailedReason").WaitAsync(cancellationToken);
        return (MMModemStateFailedReason)val;
    }

    public async Task<IReadOnlyList<MMModemAccessTechnology>> GetAccessTechnologiesAsync(CancellationToken cancellationToken = default)
    {
        var bitmask = await _proxy.GetAsync<uint>("AccessTechnologies").WaitAsync(cancellationToken);
        return MMModemAccessTechnologyExt.BitmaskToSlice(bitmask);
    }

    public async Task<(uint percent, bool recent)> GetSignalQualityAsync(CancellationToken cancellationToken = default)
    {
        var res = await _proxy.GetAsync<(uint percent, bool recent)>("SignalQuality").WaitAsync(cancellationToken);
        return res;
    }

    public Task<IReadOnlyList<string>> GetOwnNumbersAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string[]>("OwnNumbers").WaitAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<string>)t.Result, cancellationToken);

    public async Task<MMModemPowerState> GetPowerStateAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("PowerState").WaitAsync(cancellationToken);
        return (MMModemPowerState)val;
    }

    public async Task<IReadOnlyList<Mode>> GetSupportedModesAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _proxy.GetAsync<(uint allowedModesBitmask, uint preferredMode)[]>("SupportedModes").WaitAsync(cancellationToken);

        var result = new List<Mode>(raw.Length);
        foreach (var mode in raw)
        {
            var allowed = MMModemModeExt.BitmaskToSlice(mode.allowedModesBitmask);
            result.Add(new Mode(allowed, (MMModemMode)mode.preferredMode));
        }
        return result;
    }

    public async Task<Mode> GetCurrentModesAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _proxy.GetAsync<(uint allowedModesBitmask, uint preferredMode)>("CurrentModes").WaitAsync(cancellationToken);
        var allowed = MMModemModeExt.BitmaskToSlice(raw.allowedModesBitmask);
        return new Mode(allowed, (MMModemMode)raw.preferredMode);
    }

    public async Task<IReadOnlyList<MMModemBand>> GetSupportedBandsAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _proxy.GetAsync<uint[]>("SupportedBands").WaitAsync(cancellationToken);
        return raw.Select(v => (MMModemBand)v).ToArray();
    }

    public async Task<IReadOnlyList<MMModemBand>> GetCurrentBandsAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _proxy.GetAsync<uint[]>("CurrentBands").WaitAsync(cancellationToken);
        return raw.Select(v => (MMModemBand)v).ToArray();
    }

    public async Task<IReadOnlyList<MMBearerIpFamily>> GetSupportedIpFamiliesAsync(CancellationToken cancellationToken = default)
    {
        var bitmask = await _proxy.GetAsync<uint>("SupportedIpFamilies").WaitAsync(cancellationToken);
        return MMBearerIpFamilyExt.BitmaskToSlice(bitmask);
    }

    public Task<IDisposable> WatchStateChangedAsync(Action<(MMModemState oldState, MMModemState newState, MMModemStateChangeReason reason)> handler, Action<Exception>? onError = null)
        => _proxy.WatchStateChangedAsync(tuple =>
            handler(((MMModemState)tuple.oldState, (MMModemState)tuple.newState, (MMModemStateChangeReason)tuple.reason)), onError);
}
