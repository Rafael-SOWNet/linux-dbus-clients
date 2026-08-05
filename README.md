# Linux D-Bus Clients for .NET

Typed .NET clients for the three Linux system daemons an embedded or appliance-style device
usually has to talk to: **NetworkManager**, **ModemManager**, and **avahi**.

They are built on [Tmds.DBus](https://github.com/tmds/Tmds.DBus) and target `net10.0`.

```
Dbus.Clients.NetworkManager    connection profiles, Wi-Fi scan/AP mode, interface state
Dbus.Clients.ModemManager      the full ModemManager interface set, typed
Dbus.Clients.Avahi             publish DNS-SD/mDNS services at runtime
```

## Why these exist

Shelling out to `nmcli`, `mmcli`, and `avahi-publish` works right up until you need a return
value. Then you are parsing human-readable output that changes between distro versions, you
have no way to react to a property change, and every call costs a process spawn.

Going straight to D-Bus fixes all three, but the raw interfaces are wide, weakly typed
(`Dictionary<string, Dictionary<string, object>>` for a connection profile), and full of
behaviour that is not written down anywhere except mailing-list threads. These packages are
the typed layer plus, more usefully, the accumulated knowledge of what those daemons
actually do — see [Things that will bite you](#things-that-will-bite-you).

## Install

```bash
dotnet add package Dbus.Clients.NetworkManager
dotnet add package Dbus.Clients.ModemManager
dotnet add package Dbus.Clients.Avahi
```

Take only what you need — the three packages are independent and share no code.

## Avahi — publish a service

Use this over a static file in `/etc/avahi/services` when the TXT records depend on runtime
state (build version, hardware serial, which port you actually bound to).

```csharp
await using var avahi = await AvahiClient.CreateAsync();

await avahi.PublishServiceAsync(
    serviceName: "My Device",
    serviceType: "_http._tcp",
    port: 8080,
    txtRecords: new Dictionary<string, string>
    {
        ["version"] = ThisAssembly.InformationalVersion,
        ["serial"]  = boardSerial,
        ["api"]     = "/api/v1",
    });

// Report THIS to the user, not Environment.MachineName — see below.
var hostName = await avahi.GetPublishedHostNameAsync();
Console.WriteLine($"Reachable at {hostName}.local:8080");
```

Creating an entry group is privileged. If your service does not run as root, add a D-Bus
policy drop-in under `/etc/dbus-1/system.d/` allowing your user to
`org.freedesktop.Avahi.Server.EntryGroupNew`, or the call throws `DBusException`
("Access denied").

## NetworkManager — connection profiles

`UpsertEthernetProfilesAsync` builds and applies a whole addressing profile in one call.
The link-local fallback is the interesting part: a second, lower-priority profile that
activates only when DHCP fails, which is what makes a device reachable over a direct
ethernet cable with no DHCP server present.

```csharp
await using var nm = await NetworkManagerClient.CreateAsync();

await nm.UpsertEthernetProfilesAsync(new EthernetProfileRequestDto
{
    InterfaceName = "eth0",
    BaseConnectionId = "eth0",
    Mode = EthernetAddressingMode.Dhcp,
    AddLinkLocalFallbackProfile = true,   // adds "eth0-ll" at a lower autoconnect priority
});
```

Wi-Fi, in either direction:

```csharp
var aps = await nm.ListWifiAccessPointsAsync(requestScan: true);

await nm.ApplyWifiModeAsync(new WifiModeRequestDto
{
    Mode = WifiProfileMode.AccessPoint,
    AccessPoint = new WifiAccessPointProfileRequestDto { /* ssid, psk, band... */ },
});
```

Also available: `ListConnectionSummariesAsync`, `GetConnectionConfigurationByIdAsync`,
`ActivateConnectionByIdAsync`, `DeleteConnectionAsync`, `ReapplyDeviceConnectionAsync`,
`GetInterfaceRuntimeStateAsync`, `SetWifiEnabledAsync`, `SetNetworkingEnabledAsync`, and
`UpsertGsmProfileAsync` for cellular.

## ModemManager — enumerate and query

```csharp
await using var mm = await ModemManagerClient.CreateAsync();

Console.WriteLine($"ModemManager {await mm.GetVersionAsync()}");

foreach (var modem in await mm.GetModemsAsync())
{
    // ModemClient, plus Modem3gppClient, ModemSignalClient, ModemLocationClient,
    // SmsClient, CallClient, BearerClient, SimClient, ... one per MM interface.
}
```

The enumerations (`MMModemState`, `MMModemAccessTechnology`, `MMModem3gppRegistrationState`,
and the rest) are translated to C# enums rather than left as raw `uint`.

## Things that will bite you

Documented here because each one cost real debugging time on real hardware.

**Avahi renames your host on collision, silently.** If another device on the link already
publishes `mydevice.local`, avahi publishes you as `mydevice-2.local` instead — and never
tells you. `Environment.MachineName` still reports `mydevice`. Any UI or log line that tells
a user where to connect must use `GetPublishedHostNameAsync()`, or you will confidently print
an address that resolves to a different machine. This happens the moment two boards ship with
the same default hostname.

**Restarting avahi-daemon drops D-Bus-published records permanently.** The daemon discards
every entry group it was holding. Services declared in `/etc/avahi/services` come straight
back; yours does not, because nothing re-creates it. Worse, the host name is unchanged across
the restart, so a watchdog that only compares host names concludes nothing happened. Poll
`GetEntryGroupStateAsync()` and republish when it stops returning
`AvahiConstants.EntryGroupEstablished`.

In practice you usually detect the restart by that call *throwing* rather than by its return
value: the entry group object dies with the daemon, so the proxy call fails with
`UnknownObject: Method GetState ... doesn't exist` before any state comes back. Both outcomes
want the same recovery, so handle the exception as "republish" rather than letting it escape.

**Don't filter interfaces in code.** `AvahiConstants.InterfaceUnspecified` (`AVAHI_IF_UNSPEC`)
looks broad but is already bounded by `allow-interfaces` in `avahi-daemon.conf`. Keep that
policy in the daemon config only — implementing it in both places guarantees they drift.

**Empty TXT values are dropped, not published as `key=`.** A consumer cannot distinguish
"present but empty" from "absent" in any useful way, and an empty field reads as real data
when it is actually a gap.

**A D-Bus connection is not free to leak.** All three clients are `IAsyncDisposable` and own
their `Connection`. If you create one per retry in a startup loop — or one per tick in a status
poller — you leak a socket and a reader task per attempt. Create once, keep it, dispose on
shutdown.

**Leaking them takes down the whole process, not just your client.** A bus caps one UID at
`max_connections_per_user`, 256 by default. Cross it and *every* D-Bus consumer in the process
starts failing with "The maximum number of active connections for UID … has been reached" —
including clients that were never involved in the leak — and nothing recovers until the process
restarts. A once-per-second poller reaches that ceiling in about four minutes, so this shows up
as a service that works after a restart and mysteriously stops a few minutes later.

## Platform

Linux with a D-Bus system bus, and the corresponding daemon installed. The packages compile
on Windows and macOS — CI builds them there, and it keeps editor tooling working in a mixed
team — but every call needs a real system bus at runtime.

Tested against NetworkManager 1.42+, ModemManager 1.20+, and avahi 0.8 on Debian 12/13
(including Raspberry Pi OS on arm64).

## Contributing

Bug reports and PRs welcome. Two requests:

- If you add a workaround for daemon behaviour, write down *what you observed* in the doc
  comment, not just what the code does. That is the part of this repo worth having.
- Proxy interfaces under `Dbus/Proxies.cs` mirror the daemon's published introspection XML.
  Keep them faithful to it; put convenience on the `*Client` classes instead.

See [AGENTS.md](AGENTS.md) for repository layout and conventions.

## License

MIT — see [LICENSE](LICENSE).
