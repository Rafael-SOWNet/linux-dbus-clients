using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemOmaClient
{
    private readonly IModemOma _proxy;

    public ModemOmaClient(Connection connection, ObjectPath modemObjectPath)
    {
        _proxy = connection.CreateProxy<IModemOma>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public Task SetupAsync(IEnumerable<MMOmaFeature> features)
        => _proxy.SetupAsync(MMOmaFeatureExt.SliceToBitmask(features));

    public Task StartClientInitiatedSessionAsync(MMOmaSessionType sessionType)
        => _proxy.StartClientInitiatedSessionAsync((uint)sessionType);

    public Task AcceptNetworkInitiatedSessionAsync(uint sessionId, bool accept)
        => _proxy.AcceptNetworkInitiatedSessionAsync(sessionId, accept);

    public Task CancelSessionAsync() => _proxy.CancelSessionAsync();

    public async Task<IReadOnlyList<MMOmaFeature>> GetFeaturesAsync(CancellationToken cancellationToken = default)
        => MMOmaFeatureExt.BitmaskToSlice(await _proxy.GetAsync<uint>("Features").WaitAsync(cancellationToken));

    public async Task<IReadOnlyList<ModemOmaInitiatedSession>> GetPendingNetworkInitiatedSessionsAsync(CancellationToken cancellationToken = default)
    {
        var arr = await _proxy.GetAsync<(uint sessionType, uint sessionId)[]>("PendingNetworkInitiatedSessions").WaitAsync(cancellationToken);
        return arr.Select(x => new ModemOmaInitiatedSession((MMOmaSessionType)x.sessionType, x.sessionId)).ToArray();
    }

    public async Task<MMOmaSessionType> GetSessionTypeAsync(CancellationToken cancellationToken = default)
        => (MMOmaSessionType)await _proxy.GetAsync<uint>("SessionType").WaitAsync(cancellationToken);

    public async Task<MMOmaSessionState> GetSessionStateAsync(CancellationToken cancellationToken = default)
        => (MMOmaSessionState)await _proxy.GetAsync<int>("SessionState").WaitAsync(cancellationToken);

    public Task<IDisposable> WatchSessionStateChangedAsync(Action<(MMOmaSessionState oldState, MMOmaSessionState newState, MMOmaSessionStateFailedReason failureReason)> handler, Action<Exception>? onError = null)
        => _proxy.WatchSessionStateChangedAsync(tuple => handler(((MMOmaSessionState)tuple.oldState, (MMOmaSessionState)tuple.newState, (MMOmaSessionStateFailedReason)tuple.failureReason)), onError);
}
