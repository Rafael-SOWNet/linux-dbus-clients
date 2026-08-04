using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemCdmaClient
{
    private readonly IModemCdma _proxy;

    public ModemCdmaClient(Connection connection, ObjectPath modemObjectPath)
    {
        _proxy = connection.CreateProxy<IModemCdma>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public Task ActivateAsync(string carrierCode) => _proxy.ActivateAsync(carrierCode);

    public Task ActivateManualAsync(CdmaProperty property) => _proxy.ActivateManualAsync(property);

    public async Task<MMModemCdmaActivationState> GetActivationStateAsync(CancellationToken cancellationToken = default)
        => (MMModemCdmaActivationState)await _proxy.GetAsync<uint>("ActivationState").WaitAsync(cancellationToken);

    public Task<string> GetMeidAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Meid").WaitAsync(cancellationToken);

    public Task<string> GetEsnAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Esn").WaitAsync(cancellationToken);

    public Task<uint> GetSidAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<uint>("Sid").WaitAsync(cancellationToken);

    public Task<uint> GetNidAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<uint>("Nid").WaitAsync(cancellationToken);

    public async Task<MMModemCdmaRegistrationState> GetCdma1xRegistrationStateAsync(CancellationToken cancellationToken = default)
        => (MMModemCdmaRegistrationState)await _proxy.GetAsync<uint>("Cdma1xRegistrationState").WaitAsync(cancellationToken);

    public async Task<MMModemCdmaRegistrationState> GetEvdoRegistrationStateAsync(CancellationToken cancellationToken = default)
        => (MMModemCdmaRegistrationState)await _proxy.GetAsync<uint>("EvdoRegistrationState").WaitAsync(cancellationToken);

    public Task<IDisposable> WatchActivationStateChangedAsync(Action<(MMModemCdmaActivationState state, MMCdmaActivationError error, IDictionary<string, object> statusChanges)> handler, Action<Exception>? onError = null)
        => _proxy.WatchActivationStateChangedAsync(tuple => handler(((MMModemCdmaActivationState)tuple.activationState, (MMCdmaActivationError)tuple.activationError, tuple.statusChanges)), onError);
}
