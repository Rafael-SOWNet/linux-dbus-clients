using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

// Note: Tmds.DBus models DBus properties by adding GetAsync/WatchPropertiesAsync methods.
// We keep those generic property methods on each remote interface to allow simple wrapper calls.

[DBusInterface(MmConstants.ModemManagerInterface)]
public interface IModemManager1 : IDBusObject
{
    Task ScanDevicesAsync();

    Task SetLoggingAsync(string level);

    Task ReportKernelEventAsync(EventProperties properties);

    Task InhibitDeviceAsync(string uid, bool inhibit);

    Task<T> GetAsync<T>(string prop);

    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface(MmConstants.ObjectManagerInterface)]
public interface IObjectManager : IDBusObject
{
    // a{oa{sa{sv}}}
    Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync();
}

[DBusInterface(MmConstants.ModemInterface)]
public interface IModem : IDBusObject
{
    Task EnableAsync();
    Task DisableAsync();

    Task<ObjectPath> CreateBearerAsync(BearerProperty properties);
    Task DeleteBearerAsync(ObjectPath bearer);

    Task ResetAsync();
    Task FactoryResetAsync(string code);

    Task SetPowerStateAsync(uint state);
    Task SetCurrentCapabilitiesAsync(uint capabilitiesBitmask);
    Task SetCurrentModesAsync((uint allowedModesBitmask, uint preferredMode) modes);
    Task SetCurrentBandsAsync(uint[] bands);

    Task<string> CommandAsync(string cmd, uint timeout);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);

    Task<IDisposable> WatchStateChangedAsync(Action<(int oldState, int newState, uint reason)> handler, Action<Exception>? onError = null);
}

[DBusInterface(MmConstants.BearerInterface)]
public interface IBearer : IDBusObject
{
    Task ConnectAsync();
    Task DisconnectAsync();

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface(MmConstants.SimInterface)]
public interface ISim : IDBusObject
{
    Task SendPinAsync(string pin);
    Task SendPukAsync(string pin, string puk);
    Task EnablePinAsync(string pin, bool enable);
    Task ChangePinAsync(string oldPin, string newPin);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface(MmConstants.SmsInterface)]
public interface ISms : IDBusObject
{
    Task SendAsync();
    Task StoreAsync(uint storage);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface(MmConstants.CallInterface)]
public interface ICall : IDBusObject
{
    Task StartAsync();
    Task AcceptAsync();
    Task DeflectAsync(string number);
    Task JoinMultipartyAsync();
    Task LeaveMultipartyAsync();
    Task HangupAsync();
    Task SendDtmfAsync(string dtmf);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Modem3gpp")]
public interface IModem3gpp : IDBusObject
{
    Task RegisterAsync(string operatorId);
    Task<IDictionary<string, object>[]> ScanAsync();
    Task SetEpsUeModeOperationAsync(uint mode);
    Task SetInitialEpsBearerSettingsAsync(BearerProperty properties);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Signal")]
public interface IModemSignal : IDBusObject
{
    Task SetupAsync(uint rate);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Location")]
public interface IModemLocation : IDBusObject
{
    Task SetupAsync(uint sourcesBitmask, bool signalLocation);
    Task<IDictionary<uint, object>> GetLocationAsync();
    Task SetSuplServerAsync(string supl);
    Task InjectAssistanceDataAsync(byte[] data);
    Task SetGpsRefreshRateAsync(uint rate);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Simple")]
public interface IModemSimple : IDBusObject
{
    Task<ObjectPath> ConnectAsync(SimpleProperties properties);
    Task DisconnectAsync(ObjectPath bearer);
    Task<IDictionary<string, object>> GetStatusAsync();
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Messaging")]
public interface IModemMessaging : IDBusObject
{
    Task<ObjectPath[]> ListAsync();
    Task DeleteAsync(ObjectPath sms);
    Task<ObjectPath> CreateAsync(IDictionary<string, object> properties);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchAddedAsync(Action<(ObjectPath path, bool received)> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchDeletedAsync(Action<ObjectPath> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Time")]
public interface IModemTime : IDBusObject
{
    Task<string> GetNetworkTimeAsync();

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchNetworkTimeChangedAsync(Action<string> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Firmware")]
public interface IModemFirmware : IDBusObject
{
    Task<(string selected, IDictionary<string, object>[] installed)> ListAsync();
    Task SelectAsync(string uniqueId);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Voice")]
public interface IModemVoice : IDBusObject
{
    Task<ObjectPath[]> ListCallsAsync();
    Task DeleteCallAsync(ObjectPath call);
    Task<ObjectPath> CreateCallAsync(IDictionary<string, object> properties);

    Task HoldAndAcceptAsync();
    Task HangupAndAcceptAsync();
    Task HangupAllAsync();
    Task TransferAsync();
    Task CallWaitingSetupAsync(bool enable);
    Task CallWaitingQueryAsync(bool status);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchCallAddedAsync(Action<ObjectPath> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchCallDeletedAsync(Action<ObjectPath> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Modem3gpp.Ussd")]
public interface IUssd : IDBusObject
{
    Task<string> InitiateAsync(string command);
    Task<string> RespondAsync(string response);
    Task CancelAsync();

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.Oma")]
public interface IModemOma : IDBusObject
{
    Task SetupAsync(uint featuresBitmask);
    Task StartClientInitiatedSessionAsync(uint sessionType);
    Task AcceptNetworkInitiatedSessionAsync(uint sessionId, bool accept);
    Task CancelSessionAsync();

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchSessionStateChangedAsync(Action<(int oldState, int newState, uint failureReason)> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.freedesktop.ModemManager1.Modem.ModemCdma")]
public interface IModemCdma : IDBusObject
{
    Task ActivateAsync(string carrierCode);
    Task ActivateManualAsync(CdmaProperty properties);

    Task<T> GetAsync<T>(string prop);
    Task<IDisposable> WatchActivationStateChangedAsync(Action<(uint activationState, uint activationError, IDictionary<string, object> statusChanges)> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception>? onError = null);
}

