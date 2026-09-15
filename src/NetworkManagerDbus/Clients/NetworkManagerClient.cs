using NetworkManagerDbus.Builders;
using NetworkManagerDbus.Dbus;
using NetworkManagerDbus.Dto;
using NetworkManagerDbus.Mappers;
using Tmds.DBus;

namespace NetworkManagerDbus.Clients;

public sealed class NetworkManagerClient : IAsyncDisposable
{
    private readonly Connection _connection;
    private readonly INetworkManagerProxy _networkManager;
    private readonly INetworkManagerSettingsProxy _settings;

    private NetworkManagerClient(Connection connection)
    {
        _connection = connection;
        _networkManager = _connection.CreateProxy<INetworkManagerProxy>(NmConstants.NetworkManagerService, NmConstants.NetworkManagerPath);
        _settings = _connection.CreateProxy<INetworkManagerSettingsProxy>(NmConstants.NetworkManagerService, NmConstants.NetworkManagerSettingsPath);
    }

    /// <summary>Connects to the system bus and returns a client owning that connection.</summary>
    /// <remarks>
    /// The connection is disposed if connecting fails. Until it reaches the client nothing else
    /// can release it — the caller receives an exception rather than an object — so a connection
    /// leaked here is unreachable for the rest of the process's life.
    ///
    /// That matters most precisely when this is most likely to fail: a bus refuses a UID that has
    /// reached <c>max_connections_per_user</c> (256 by default), so a process near the ceiling
    /// throws on every further attempt, and each of those would otherwise leak one more and push
    /// it further past the limit.
    /// </remarks>
    public static async Task<NetworkManagerClient> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new Connection(Address.System);
        try
        {
            await connection.ConnectAsync().WaitAsync(cancellationToken);
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        return new NetworkManagerClient(connection);
    }

    public ValueTask DisposeAsync()
    {
        _connection.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// UUIDs of every currently activated or activating connection.
    ///
    /// Exists so a caller can avoid rewriting a profile that NetworkManager is in the middle of
    /// bringing up. Updating a connection during its own activation makes NetworkManager abandon
    /// the attempt with "The connection was modified since activation", reported as reason
    /// no-secrets - which is terminal, so autoconnect never retries and the interface simply stays
    /// down. Observed on gme-merge 2026-09-08, where saving Wi-Fi client settings activated the
    /// profile and then rewrote it, leaving wlan0 disconnected with a correct PSK on disk.
    ///
    /// A connection that disappears mid-enumeration is skipped rather than throwing: the list is a
    /// snapshot of a live daemon, and any caller is racing it by definition.
    /// </summary>
    public async Task<IReadOnlySet<string>> ListActiveConnectionUuidsAsync(CancellationToken cancellationToken = default)
    {
        var uuids = new HashSet<string>(StringComparer.Ordinal);

        ObjectPath[] paths;
        try
        {
            paths = await _networkManager.GetAsync<ObjectPath[]>("ActiveConnections").WaitAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return uuids;
        }

        foreach (var path in paths)
        {
            try
            {
                var proxy = _connection.CreateProxy<INetworkManagerActiveConnectionProxy>(
                    NmConstants.NetworkManagerService, path);
                var uuid = await proxy.GetAsync<string>("Uuid").WaitAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(uuid))
                    uuids.Add(uuid);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Deactivated between the listing and the read. Not active any more, so skipping it
                // is the correct answer rather than a lost result.
            }
        }

        return uuids;
    }

    public async Task<IReadOnlyList<ConnectionSummaryDto>> ListConnectionSummariesAsync(CancellationToken cancellationToken = default)
    {
        var paths = await _settings.ListConnectionsAsync().WaitAsync(cancellationToken);
        var list = new List<ConnectionSummaryDto>(paths.Length);

        foreach (var path in paths)
        {
            var summary = await GetConnectionSummaryAsync(path, cancellationToken);
            list.Add(summary);
        }

        return list;
    }

    public async Task<ObjectPath> AddConnectionAsync(NetworkConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        var settings = ConnectionSettingsBuilder.Build(dto);
        return await _settings.AddConnectionAsync(settings).WaitAsync(cancellationToken);
    }

    public async Task UpdateConnectionAsync(ObjectPath connectionPath, NetworkConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        var proxy = _connection.CreateProxy<INetworkManagerConnectionProxy>(NmConstants.NetworkManagerService, connectionPath);
        var settings = ConnectionSettingsBuilder.Build(dto);
        await proxy.UpdateAsync(settings).WaitAsync(cancellationToken);
    }

    public async Task<ObjectPath> UpsertConnectionAsync(NetworkConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await FindConnectionByIdAsync(dto.ConnectionId, cancellationToken);
        if (existing is null)
            return await AddConnectionAsync(dto, cancellationToken);

        await UpdateConnectionAsync(existing.Value, dto, cancellationToken);
        return existing.Value;
    }

    public async Task<IReadOnlyList<ObjectPath>> UpsertEthernetProfilesAsync(EthernetProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var profiles = EthernetProfileFactory.Create(request);
        var result = new List<ObjectPath>(profiles.Count);

        foreach (var profile in profiles)
            result.Add(await UpsertConnectionAsync(profile, cancellationToken));

        return result;
    }

    public Task<ObjectPath> UpsertWifiClientProfileAsync(WifiClientProfileRequestDto request, CancellationToken cancellationToken = default)
        => UpsertConnectionAsync(WifiProfileFactory.CreateClientProfile(request), cancellationToken);

    public Task<ObjectPath> UpsertWifiAccessPointProfileAsync(WifiAccessPointProfileRequestDto request, CancellationToken cancellationToken = default)
        => UpsertConnectionAsync(WifiProfileFactory.CreateAccessPointProfile(request), cancellationToken);

    public Task<ObjectPath> ApplyWifiModeAsync(WifiModeRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Mode switch
        {
            WifiProfileMode.Client when request.Client is not null => UpsertWifiClientProfileAsync(request.Client, cancellationToken),
            WifiProfileMode.AccessPoint when request.AccessPoint is not null => UpsertWifiAccessPointProfileAsync(request.AccessPoint, cancellationToken),
            WifiProfileMode.Client => throw new InvalidOperationException("Client mode requires Client payload."),
            WifiProfileMode.AccessPoint => throw new InvalidOperationException("AccessPoint mode requires AccessPoint payload."),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Mode), request.Mode, null),
        };
    }

    public Task<ObjectPath> UpsertGsmProfileAsync(GsmProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ConnectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Apn))
            throw new ArgumentException("APN is required.", nameof(request));

        return UpsertConnectionAsync(new NetworkConfigurationDto
        {
            ConnectionType = "gsm",
            ConnectionId = request.ConnectionId,
            InterfaceName = request.InterfaceName,
            ConnectionOptions = new ConnectionOptionsDto
            {
                Autoconnect = true,
                AutoconnectPriority = request.AutoconnectPriority,
            },
            Gsm = new GsmConfigurationDto
            {
                Apn = request.Apn,
            },
            Ipv4 = new IpConfigurationDto
            {
                Enabled = true,
                Method = IpMethod.Auto,
                LinkLocal = 1,
            },
            Ipv6 = request.EnableIpv6
                ? new IpConfigurationDto { Enabled = true, Method = IpMethod.Auto }
                : new IpConfigurationDto { Enabled = false, Method = IpMethod.Ignore },
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<WifiAccessPointDto>> ListWifiAccessPointsAsync(string? interfaceName = null, bool requestScan = true, CancellationToken cancellationToken = default)
    {
        var devicePaths = await _networkManager.GetDevicesAsync().WaitAsync(cancellationToken);
        var result = new List<WifiAccessPointDto>();

        foreach (var devicePath in devicePaths)
        {
            var device = _connection.CreateProxy<INetworkManagerDeviceProxy>(NmConstants.NetworkManagerService, devicePath);
            var deviceType = await device.GetAsync<uint>("DeviceType").WaitAsync(cancellationToken);
            if (deviceType != NmConstants.DeviceTypeWifi)
                continue;

            var iface = await device.GetAsync<string>("Interface").WaitAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(interfaceName) && !string.Equals(interfaceName, iface, StringComparison.Ordinal))
                continue;

            var wireless = _connection.CreateProxy<INetworkManagerWirelessDeviceProxy>(NmConstants.NetworkManagerService, devicePath);
            if (requestScan)
                await ScanAndWaitAsync(wireless, cancellationToken);

            var apPaths = await wireless.GetAsync<ObjectPath[]>("AccessPoints").WaitAsync(cancellationToken);
            foreach (var apPath in apPaths)
            {
                var ap = _connection.CreateProxy<INetworkManagerAccessPointProxy>(NmConstants.NetworkManagerService, apPath);

                byte[] ssidBytes;
                try
                {
                    ssidBytes = await ap.GetAsync<byte[]>("Ssid").WaitAsync(cancellationToken);
                }
                catch
                {
                    continue;
                }

                var ssid = DecodeSsid(ssidBytes);
                var strength = await SafeGet(ap, "Strength", (byte)0, cancellationToken);
                var bssid = await SafeGet<string?>(ap, "HwAddress", null, cancellationToken);
                var frequency = await SafeGet<uint?>(ap, "Frequency", null, cancellationToken);
                var flags = await SafeGet<uint>(ap, "Flags", 0u, cancellationToken);
                var wpaFlags = await SafeGet<uint>(ap, "WpaFlags", 0u, cancellationToken);
                var rsnFlags = await SafeGet<uint>(ap, "RsnFlags", 0u, cancellationToken);

                result.Add(new WifiAccessPointDto
                {
                    DeviceInterface = iface,
                    ObjectPath = apPath.ToString(),
                    Ssid = ssid,
                    Bssid = bssid,
                    Strength = strength,
                    FrequencyMhz = frequency,
                    Secured = flags != 0 || wpaFlags != 0 || rsnFlags != 0,
                });
            }
        }

        return result
            .OrderByDescending(a => a.Strength)
            .ThenBy(a => a.Ssid, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<bool> GetWifiEnabledAsync(CancellationToken cancellationToken = default)
        => await _networkManager.GetAsync<bool>("WirelessEnabled").WaitAsync(cancellationToken);

    public async Task<bool> GetWifiHardwareEnabledAsync(CancellationToken cancellationToken = default)
        => await _networkManager.GetAsync<bool>("WirelessHardwareEnabled").WaitAsync(cancellationToken);

    public Task SetWifiEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _networkManager.SetAsync("WirelessEnabled", enabled).WaitAsync(cancellationToken);

    public Task SetNetworkingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _networkManager.EnableAsync(enabled).WaitAsync(cancellationToken);

    public async Task ReapplyDeviceConnectionAsync(string interfaceName, CancellationToken cancellationToken = default)
    {
        var devicePath = await FindDeviceByInterfaceAsync(interfaceName, cancellationToken);
        if (devicePath is null)
            return;

        var device = _connection.CreateProxy<INetworkManagerDeviceProxy>(NmConstants.NetworkManagerService, devicePath.Value);
        await device.ReapplyAsync(new Dictionary<string, IDictionary<string, object>>(), 0UL, 0U).WaitAsync(cancellationToken);
    }

    public async Task DeleteConnectionAsync(ObjectPath connectionPath, CancellationToken cancellationToken = default)
    {
        var proxy = _connection.CreateProxy<INetworkManagerConnectionProxy>(NmConstants.NetworkManagerService, connectionPath);
        await proxy.DeleteAsync().WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Activates a connection on the device that has the given interface name.
    /// If <paramref name="interfaceName"/> is null or empty, NM auto-selects the device.
    /// </summary>
    public async Task ActivateConnectionByIdAsync(string connectionId, string? interfaceName, CancellationToken cancellationToken = default)
    {
        var connectionPath = await FindConnectionByIdAsync(connectionId, cancellationToken);
        if (connectionPath is null)
            return;

        var devicePath = new ObjectPath("/");
        if (!string.IsNullOrWhiteSpace(interfaceName))
        {
            var found = await FindDeviceByInterfaceAsync(interfaceName, cancellationToken);
            if (found is not null)
                devicePath = found.Value;
        }

        await _networkManager.ActivateConnectionAsync(connectionPath.Value, devicePath, new ObjectPath("/")).WaitAsync(cancellationToken);
    }

    private async Task<ObjectPath?> FindDeviceByInterfaceAsync(string interfaceName, CancellationToken cancellationToken)
    {
        var devicePaths = await _networkManager.GetDevicesAsync().WaitAsync(cancellationToken);
        foreach (var devicePath in devicePaths)
        {
            var device = _connection.CreateProxy<INetworkManagerDeviceProxy>(NmConstants.NetworkManagerService, devicePath);
            var iface = await device.GetAsync<string>("Interface").WaitAsync(cancellationToken);
            if (string.Equals(iface, interfaceName, StringComparison.Ordinal))
                return devicePath;
        }
        return null;
    }

    public async Task<ObjectPath?> FindConnectionByIdAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("Connection id is required.", nameof(connectionId));

        var paths = await _settings.ListConnectionsAsync().WaitAsync(cancellationToken);
        foreach (var path in paths)
        {
            var summary = await GetConnectionSummaryAsync(path, cancellationToken);
            if (string.Equals(summary.Id, connectionId, StringComparison.Ordinal))
                return path;
        }

        return null;
    }

    public async Task<ConnectionSummaryDto> GetConnectionSummaryAsync(ObjectPath connectionPath, CancellationToken cancellationToken = default)
    {
        var proxy = _connection.CreateProxy<INetworkManagerConnectionProxy>(NmConstants.NetworkManagerService, connectionPath);
        var settings = await proxy.GetSettingsAsync().WaitAsync(cancellationToken);

        if (!settings.TryGetValue("connection", out var section))
        {
            return new ConnectionSummaryDto
            {
                ObjectPath = connectionPath.ToString(),
            };
        }

        return new ConnectionSummaryDto
        {
            ObjectPath = connectionPath.ToString(),
            Id = ReadString(section, "id") ?? string.Empty,
            Uuid = ReadString(section, "uuid"),
            Type = ReadString(section, "type"),
            InterfaceName = ReadString(section, "interface-name"),
            Autoconnect = ReadBool(section, "autoconnect"),
        };
    }

    /// <summary>
    /// Setting groups whose secrets this client models on <see cref="NetworkConfigurationDto"/>.
    /// Anything not listed here has no secret field to lose.
    /// </summary>
    private static readonly string[] SecretBearingSettings = ["802-11-wireless-security", "gsm"];

    /// <param name="includeSecrets">
    /// Merge secrets into the result. Required whenever the result will be written back with
    /// <see cref="UpdateConnectionAsync"/> or <see cref="UpsertConnectionAsync"/> - see the remarks
    /// on the overload below.
    /// </param>
    public async Task<NetworkConfigurationDto> GetConnectionConfigurationAsync(ObjectPath connectionPath, bool includeSecrets, CancellationToken cancellationToken = default)
    {
        var proxy = _connection.CreateProxy<INetworkManagerConnectionProxy>(NmConstants.NetworkManagerService, connectionPath);
        var settings = await proxy.GetSettingsAsync().WaitAsync(cancellationToken);

        if (includeSecrets)
            settings = await MergeSecretsAsync(proxy, settings, cancellationToken);

        return ConnectionSettingsMapper.Map(settings);
    }

    public Task<NetworkConfigurationDto> GetConnectionConfigurationAsync(ObjectPath connectionPath, CancellationToken cancellationToken = default)
        => GetConnectionConfigurationAsync(connectionPath, includeSecrets: false, cancellationToken);

    /// <remarks>
    /// A read-modify-write cycle MUST pass <paramref name="includeSecrets"/> = true. NetworkManager
    /// omits secrets from GetSettings and its Update replaces the whole connection, so reading
    /// without secrets and writing back deletes them. For a Wi-Fi access point that turns a
    /// WPA2-protected AP into an open one - observed live on gme-merge on 2026-09-08, where
    /// flipping autoconnect on an unrelated profile stripped key-mgmt and psk from
    /// "wifi-ap-bridge" and left it beaconing unauthenticated on a bridged interface.
    /// </remarks>
    public async Task<NetworkConfigurationDto?> GetConnectionConfigurationByIdAsync(string connectionId, bool includeSecrets, CancellationToken cancellationToken = default)
    {
        var path = await FindConnectionByIdAsync(connectionId, cancellationToken);
        if (path is null)
            return null;

        return await GetConnectionConfigurationAsync(path.Value, includeSecrets, cancellationToken);
    }

    public Task<NetworkConfigurationDto?> GetConnectionConfigurationByIdAsync(string connectionId, CancellationToken cancellationToken = default)
        => GetConnectionConfigurationByIdAsync(connectionId, includeSecrets: false, cancellationToken);

    /// <summary>
    /// Overlays each secret-bearing setting group's secrets onto the settings dictionary.
    ///
    /// A connection that carries no secrets, or whose secrets this agent may not read, answers with
    /// an error or an empty dictionary; both are non-fatal and leave the group as GetSettings
    /// returned it.
    /// </summary>
    private static async Task<IDictionary<string, IDictionary<string, object>>> MergeSecretsAsync(
        INetworkManagerConnectionProxy proxy,
        IDictionary<string, IDictionary<string, object>> settings,
        CancellationToken cancellationToken)
    {
        foreach (var settingName in SecretBearingSettings)
        {
            if (!settings.ContainsKey(settingName))
                continue;

            IDictionary<string, IDictionary<string, object>>? secrets = null;
            try
            {
                secrets = await proxy.GetSecretsAsync(settingName).WaitAsync(cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // No secrets stored, or not readable by this agent. Nothing to merge.
            }

            if (secrets is null || !secrets.TryGetValue(settingName, out var secretValues))
                continue;

            foreach (var (key, value) in secretValues)
                settings[settingName][key] = value;
        }

        return settings;
    }

    public async Task<InterfaceRuntimeStateDto?> GetInterfaceRuntimeStateAsync(string interfaceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            throw new ArgumentException("Interface name is required.", nameof(interfaceName));

        var devicePaths = await _networkManager.GetDevicesAsync().WaitAsync(cancellationToken);
        foreach (var devicePath in devicePaths)
        {
            var device = _connection.CreateProxy<INetworkManagerDeviceProxy>(NmConstants.NetworkManagerService, devicePath);
            var iface = await device.GetAsync<string>("Interface").WaitAsync(cancellationToken);
            if (!string.Equals(iface, interfaceName, StringComparison.Ordinal))
                continue;

            var state = new InterfaceRuntimeStateDto { InterfaceName = iface };

            var ip4Path = await device.GetAsync<ObjectPath>("Ip4Config").WaitAsync(cancellationToken);
            if (!IsRootPath(ip4Path))
            {
                state.HasIpv4Config = true;
                var ip4 = _connection.CreateProxy<INetworkManagerIp4ConfigProxy>(NmConstants.NetworkManagerService, ip4Path);
                var ip4AddressData = await SafeGetAddressDataAsync(ip4, cancellationToken);
                state.Ipv4LinkLocalAddress = ip4AddressData
                    .Select(a => a.Address)
                    .FirstOrDefault(a => a?.StartsWith("169.254.", StringComparison.Ordinal) == true);
            }

            var ip6Path = await device.GetAsync<ObjectPath>("Ip6Config").WaitAsync(cancellationToken);
            if (!IsRootPath(ip6Path))
            {
                state.HasIpv6Config = true;
                var ip6 = _connection.CreateProxy<INetworkManagerIp6ConfigProxy>(NmConstants.NetworkManagerService, ip6Path);
                var ip6AddressData = await SafeGetAddressDataAsync(ip6, cancellationToken);
                state.Ipv6LinkLocalAddress = ip6AddressData
                    .Select(a => a.Address)
                    .FirstOrDefault(a => a?.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase) == true);
            }

            return state;
        }

        return null;
    }

    private static string? ReadString(IDictionary<string, object> section, string key)
    {
        if (!section.TryGetValue(key, out var value))
            return null;

        return value as string;
    }

    private static bool? ReadBool(IDictionary<string, object> section, string key)
    {
        if (!section.TryGetValue(key, out var value))
            return null;

        return value as bool?;
    }

    private static string DecodeSsid(byte[] raw)
    {
        if (raw.Length == 0)
            return string.Empty;

        return System.Text.Encoding.UTF8.GetString(raw).TrimEnd('\0');
    }

    private static async Task<T> SafeGet<T>(INetworkManagerAccessPointProxy ap, string property, T fallback, CancellationToken cancellationToken)
    {
        try
        {
            return await ap.GetAsync<T>(property).WaitAsync(cancellationToken);
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// Overall budget for "request a scan and wait for its results". A scan on the Pi's onboard
    /// radio normally lands well inside this; the cap exists so a wedged or absent radio degrades to
    /// the old behaviour - returning whatever NetworkManager already has - instead of hanging the
    /// HTTP request that asked for it.
    /// </summary>
    private static readonly TimeSpan WifiScanBudget = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan WifiScanPollInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Requests a Wi-Fi scan and waits for it to actually finish.
    ///
    /// Two separate things make the obvious "RequestScan, then read AccessPoints" wrong:
    ///
    /// 1. RequestScan only STARTS a scan. It returns as soon as NetworkManager accepts the request,
    ///    so reading AccessPoints straight after returns the PREVIOUS cache. LastScan
    ///    (CLOCK_BOOTTIME milliseconds, -1 when never scanned) changes when a scan COMPLETES, which
    ///    is the signal actually worth waiting on.
    ///
    /// 2. Right after the radio is switched on, wlan0 is not yet in a state that accepts a scan and
    ///    RequestScan throws. Swallowing that and reading the cache is why a scan taken immediately
    ///    after re-enabling Wi-Fi reliably found nothing at all.
    ///
    /// So the request is retried until it is accepted, and the loop exits as soon as a scan
    /// completes - both within one budget. A cooldown rejection ("scanning not allowed immediately
    /// following previous scan") lands in the same retry path and resolves the same way.
    /// </summary>
    private static async Task ScanAndWaitAsync(INetworkManagerWirelessDeviceProxy wireless, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + WifiScanBudget;
        var lastScanBefore = await TryGetLastScanAsync(wireless, cancellationToken);
        var accepted = false;

        while (DateTime.UtcNow < deadline)
        {
            if (!accepted)
            {
                try
                {
                    await wireless.RequestScanAsync(new Dictionary<string, object>()).WaitAsync(cancellationToken);
                    accepted = true;
                }
                catch
                {
                    // Device not ready yet, or a scan cooldown. Both resolve by waiting.
                }
            }

            await Task.Delay(WifiScanPollInterval, cancellationToken);

            var lastScan = await TryGetLastScanAsync(wireless, cancellationToken);

            // -1 means "never scanned", so it is not a completion. Any other change is.
            if (lastScan > 0 && lastScan != lastScanBefore)
                return;
        }
    }

    private static async Task<long> TryGetLastScanAsync(INetworkManagerWirelessDeviceProxy wireless, CancellationToken cancellationToken)
    {
        try
        {
            return await wireless.GetAsync<long>("LastScan").WaitAsync(cancellationToken);
        }
        catch
        {
            // Older NetworkManager builds do not expose LastScan. Reporting "never scanned" makes
            // the wait above fall through to its timeout, which is the previous behaviour.
            return -1;
        }
    }
    private static bool IsRootPath(ObjectPath path)
        => string.Equals(path.ToString(), "/", StringComparison.Ordinal);

    private static async Task<IReadOnlyList<AddressDataEntry>> SafeGetAddressDataAsync(INetworkManagerIp4ConfigProxy ip4, CancellationToken cancellationToken)
    {
        try
        {
            var addressData = await ip4.GetAsync<IDictionary<string, object>[]>("AddressData").WaitAsync(cancellationToken);
            return MapAddressData(addressData);
        }
        catch
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<AddressDataEntry>> SafeGetAddressDataAsync(INetworkManagerIp6ConfigProxy ip6, CancellationToken cancellationToken)
    {
        try
        {
            var addressData = await ip6.GetAsync<IDictionary<string, object>[]>("AddressData").WaitAsync(cancellationToken);
            return MapAddressData(addressData);
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<AddressDataEntry> MapAddressData(IEnumerable<IDictionary<string, object>> addressData)
    {
        var result = new List<AddressDataEntry>();
        foreach (var item in addressData)
        {
            if (!item.TryGetValue("address", out var addressRaw))
                continue;

            var address = addressRaw?.ToString();
            if (string.IsNullOrWhiteSpace(address))
                continue;

            item.TryGetValue("prefix", out var prefixRaw);
            int? prefix = prefixRaw switch
            {
                uint u => (int?)u,
                int i => i,
                long l when l is >= int.MinValue and <= int.MaxValue => (int?)l,
                _ => null,
            };

            result.Add(new AddressDataEntry(address, prefix));
        }

        return result;
    }

    private sealed record AddressDataEntry(string Address, int? Prefix);

}
