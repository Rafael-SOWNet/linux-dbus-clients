using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class SimClient
{
    private readonly ISim _proxy;
    private readonly ObjectPath _path;

    public SimClient(Connection connection, ObjectPath objectPath)
    {
        _path = objectPath;
        _proxy = connection.CreateProxy<ISim>(MmConstants.ModemManagerInterface, objectPath);
    }

    public ObjectPath ObjectPath => _path;

    public Task SendPinAsync(string pin) => _proxy.SendPinAsync(pin);

    public Task SendPukAsync(string puk, string pin) => _proxy.SendPukAsync(puk, pin);

    public Task EnablePinAsync(string pin, bool enable) => _proxy.EnablePinAsync(pin, enable);

    public Task ChangePinAsync(string oldPin, string newPin) => _proxy.ChangePinAsync(oldPin, newPin);

    public Task<string> GetSimIdentifierAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("SimIdentifier").WaitAsync(cancellationToken);

    public Task<string> GetImsiAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Imsi").WaitAsync(cancellationToken);

    public Task<string> GetOperatorIdentifierAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("OperatorIdentifier").WaitAsync(cancellationToken);

    public Task<string> GetOperatorNameAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("OperatorName").WaitAsync(cancellationToken);

    public Task<IReadOnlyList<string>> GetEmergencyNumbersAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string[]>("EmergencyNumbers").WaitAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<string>)t.Result, cancellationToken);
}
