using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class UssdClient
{
    private readonly IUssd _proxy;

    public UssdClient(Connection connection, ObjectPath modemObjectPath)
    {
        _proxy = connection.CreateProxy<IUssd>(MmConstants.ModemManagerInterface, modemObjectPath);
    }

    public Task<string> InitiateAsync(string command) => _proxy.InitiateAsync(command);
    public Task<string> RespondAsync(string response) => _proxy.RespondAsync(response);
    public Task CancelAsync() => _proxy.CancelAsync();

    public async Task<MMModem3gppUssdSessionState> GetStateAsync(CancellationToken cancellationToken = default)
        => (MMModem3gppUssdSessionState)await _proxy.GetAsync<uint>("State").WaitAsync(cancellationToken);

    public Task<string> GetNetworkNotificationAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("NetworkNotification").WaitAsync(cancellationToken);

    public Task<string> GetNetworkRequestAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("NetworkRequest").WaitAsync(cancellationToken);
}
