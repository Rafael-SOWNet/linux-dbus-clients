# AGENTS.md

Working context for AI coding agents (and humans skimming for conventions). Most of this
codebase was written with Claude, and that is expected to continue — so this file is the
authoritative brief. Read it fully before changing anything.

---

## 1. What this repository is

Three independent .NET client libraries over Linux system daemons, published as three separate
NuGet packages from one repository. They share build configuration, CI and conventions, and
**no code**. There is deliberately no common "core" package.

| Directory | Namespace / assembly | Package ID | Lines |
|---|---|---|---|
| `src/AvahiDbus/` | `AvahiDbus` | `Dbus.Clients.Avahi` | ~270 |
| `src/NetworkManagerDbus/` | `NetworkManagerDbus` | `Dbus.Clients.NetworkManager` | ~1530 |
| `src/ModemManagerDbus/` | `ModemManagerDbus` | `Dbus.Clients.ModemManager` | ~2855 |

Package IDs intentionally differ from assembly names: assembly names stay short and readable in
stack traces, package IDs are namespaced enough to be unambiguous on nuget.org. Do not "fix"
this by aligning them.

Everything targets `net10.0`, `LangVersion=preview`, nullable enabled, `IsTrimmable=true`.
The only runtime dependency is [Tmds.DBus](https://github.com/tmds/Tmds.DBus) 0.92.0.

---

## 2. Build and verify

```bash
dotnet build -c Release -warnaserror    # must be 0 warnings, 0 errors
dotnet pack  -c Release -o ./artifacts  # produces 3 .nupkg + 3 .snupkg
```

CI (`.github/workflows/ci.yml`) builds on `ubuntu-latest` **and** `windows-latest`, then packs
on Ubuntu. Tagging `v0.2.0` publishes `0.2.0` of all three packages to nuget.org, if the
`NUGET_API_KEY` secret is set; without it the publish step no-ops rather than failing.

**SourceLink requires a git remote and full history.** If you build in a fresh clone with
`--depth 1`, or in a directory that is not a git repo, `-warnaserror` fails with
"Source control information is not available". That is the tooling, not your change.

### There are no tests, and a green build proves little

Meaningful tests need a live D-Bus system bus and the daemons themselves. Mocking a
`Tmds.DBus` proxy interface would only test the mock — every bug this library exists to work
around lives in daemon behaviour, not in our dispatch code.

So: **a successful build does not mean a change to daemon-interaction logic is correct.**
Either exercise it against a real daemon, or say plainly in the PR description that it is
unverified. Do not imply verification you did not do.

If you add tests, add the harness (a container with the daemon running, or a VM), not the mocks.

### Running against a real daemon

You need Linux with a D-Bus system bus. The packages *compile* on Windows and macOS — CI proves
it, and it keeps editor tooling working in a mixed team — but every call needs a real bus.

- WSL2 does not run a system bus by default; start one, or use a container.
- Containers need `--privileged` or an explicit bind of `/var/run/dbus/system_bus_socket`, plus
  the daemon itself installed and running.
- The daemons are separately gated by policy: see §6 on privileges.

Useful for cross-checking what the daemon actually exposes, since our proxy interfaces must
mirror it:

```bash
busctl introspect org.freedesktop.NetworkManager /org/freedesktop/NetworkManager
busctl introspect org.freedesktop.Avahi /
mmcli -m 0                     # ModemManager, human-readable
nmcli connection show <id>     # NetworkManager, human-readable
```

---

## 3. Architecture — the four layers

Every project follows the same shape (except `ModemManagerDbus`, see below). The layering is
load-bearing; changes that blur it will be asked to be rewritten.

```
Dbus/Proxies.cs   [DBusInterface] interfaces mirroring the daemon's introspection XML.
      ▲           Faithful to the wire. No convenience, no reinterpretation, no merging
      │           of two D-Bus interfaces into one C# interface.
      │
Dto/*.cs          Typed models replacing D-Bus's a{sa{sv}} settings dictionaries.
      ▲           Plain data. No behaviour.
      │
Builders/         DTO → settings dictionary  (outbound)
Mappers/          settings dictionary → DTO  (inbound)
      ▲
      │
Clients/*Client.cs  The ergonomic public surface. ALL convenience lives here.
                    IAsyncDisposable, owns the Connection, applies CancellationToken.
```

`*Constants.cs`, `*Enums.cs`, `*Types.cs` hold daemon constants and enumerations, named to match
the upstream C names (`MMModemState`, `NM_DEVICE_TYPE_WIFI`) so they can be grepped against the
daemon's own documentation.

`ModemManagerDbus` is flat rather than layered: it is overwhelmingly one generated-shape client
per MM interface — 18 `[DBusInterface]` declarations in `Proxies.cs` and 17 `*Client.cs` files
(`IObjectManager` is consumed by `ModemManagerClient.GetModemsAsync` rather than getting a client
of its own). Do not restructure it for symmetry alone.

### Tmds.DBus patterns you need to know

- A proxy interface must extend `IDBusObject` and carry `[DBusInterface("org.freedesktop.…")]`.
- D-Bus properties are surfaced as `Task<T> GetAsync<T>(string prop)` / `Task SetAsync(string, object)`.
- Signals are surfaced as `Task<IDisposable> Watch…Async(Action<T> handler, Action<Exception>? onError)`.
  The returned `IDisposable` **is the subscription** — dropping it unsubscribes. Store it.
- `ObjectPath` is a struct, not a string. Paths are the identity of remote objects.
- Get a proxy with `connection.CreateProxy<IFooProxy>(serviceName, objectPath)`. This is cheap
  and local — it does no I/O and does not validate that the object exists. The first *call*
  is what fails if it doesn't.
- Signature mismatches between your interface and the daemon's real one surface at call time as
  a `DBusException`, not at compile time. This is the single most common source of bugs here.

---

## 4. The NetworkManager round-trip invariant (read before touching it)

`ConnectionSettingsBuilder` and `ConnectionSettingsMapper` are inverses over the same
`a{sa{sv}}` shape:

```
NetworkConfigurationDto ──Builder.Build()──▶ IDictionary<string, IDictionary<string, object>>
                        ◀──Mapper.Map()────
```

**Adding a field to one without the other silently loses data.** There is no test to catch it.
The Mapper not reading a key means a `GetConnectionConfigurationAsync` round-trip drops it; the
Builder not writing a key means it never reaches the daemon.

This matters more than it looks, because **NetworkManager's `Update()` replaces the entire
settings dictionary — it is not a merge.** So this sequence permanently drops any setting the
Mapper doesn't read or the Builder doesn't write, even settings you never touched:

```csharp
var dto = await nm.GetConnectionConfigurationByIdAsync("eth0");  // Mapper: dict → DTO
dto.Ipv4.Method = IpMethod.Manual;                               // your edit
await nm.UpsertConnectionAsync(dto);                             // Builder: DTO → dict, then Update()
```

Sections currently handled: `connection`, `ipv4`, `ipv6`, `bridge`, `802-11-wireless`,
`802-11-wireless-security`, `gsm`. Anything else a user configured by other means (`802-3-ethernet`
tuning, `proxy`, `match`, VPN, team/bond) is **not** preserved across an update.

### Known sharp edge — UUID regeneration on update (unverified)

`BuildConnectionSection` mints a fresh `Guid` when `dto.Uuid` is empty:

```csharp
["uuid"] = string.IsNullOrWhiteSpace(dto.Uuid) ? Guid.NewGuid().ToString() : dto.Uuid,
```

The profile factories (`EthernetProfileFactory`, `WifiProfileFactory`) do not set `Uuid`. So
`UpsertEthernetProfilesAsync` against an *existing* connection sends `Update()` with a different
UUID than the stored connection has.

Whether NetworkManager rejects that, ignores it, or accepts it as an identity change has **not
been verified against a live daemon**. If you are working in this area, check it first — and if
you determine the answer, record it here and in the doc comment rather than fixing it silently.

Identity for upsert is `ConnectionId` (the `connection.id` field), via `FindConnectionByIdAsync`
— not the UUID and not the interface name.

---

## 5. Daemon behaviour worth knowing

Each of these cost real debugging time. They are the reason this library is worth more than its
line count. They are documented in the relevant doc comments and mirrored in README.md's
"Things that will bite you" — keep all three in sync.

**Avahi renames your host on collision, silently.** If another device on the link already
publishes `mydevice.local`, avahi publishes you as `mydevice-2.local` and never tells you.
`Environment.MachineName` still says `mydevice`. Anything that reports a connect address to a
user must call `GetPublishedHostNameAsync()`. This fires the moment two boards ship with the
same default hostname.

**Restarting avahi-daemon drops D-Bus-published records permanently.** The daemon discards every
entry group it held. Services in `/etc/avahi/services` come back; yours does not, because nothing
re-creates it. The host name is unchanged across the restart, so a watchdog comparing host names
concludes nothing happened. Poll `GetEntryGroupStateAsync()` and republish when it stops
returning `EntryGroupEstablished`.

In practice you detect the restart by that call **throwing** rather than by its return value —
the entry group object dies with the daemon, so the proxy call fails with
`UnknownObject: Method GetState … doesn't exist` before any state arrives. Both outcomes want the
same recovery, so treat the exception as "republish", don't let it escape.

**Don't filter interfaces in code.** `AvahiConstants.InterfaceUnspecified` (`AVAHI_IF_UNSPEC`)
looks broad but is already bounded by `allow-interfaces` in `avahi-daemon.conf`. Keeping that
policy in one place is the point; implementing it in both guarantees drift.

**Empty TXT values are dropped, not published as `key=`.** A consumer cannot distinguish
"present but empty" from "absent", and an empty field reads as real data when it's a gap.

**A D-Bus connection is not free to leak.** All clients are `IAsyncDisposable` and own their
`Connection`. Creating one per attempt in a retry loop — or per tick in a poller — leaks a socket
and a reader task per attempt. Create once, keep it, dispose on shutdown.

The blast radius is the reason this is in this list rather than being a mere resource nit: a bus
caps one UID at `max_connections_per_user` (256 by default), and crossing it takes the bus away
from **every** D-Bus consumer in the process, including ones that never leaked anything. Nothing
recovers short of a process restart. A once-per-second poller gets there in about four minutes,
which presents as a service that is healthy after a restart and dead a few minutes later. This is
also why `CreateAsync` disposes its `Connection` when connecting fails: near the ceiling, connect
attempts are exactly what throws, and a leak on that path feeds itself.

**Link-local fallback is a second profile, not a flag.** `AddLinkLocalFallbackProfile` adds a
separate `<id>-ll` connection at a lower `autoconnect-priority` (default −100 vs +100), so
NetworkManager falls back to it only when DHCP fails. That is what keeps a device reachable over
a direct ethernet cable with no DHCP server. It is not an IPv4 method setting.

---

## 6. Privileges

These are system daemons; most interesting calls are policy-gated and fail at runtime, not at
build time.

- **Avahi**: creating an entry group is privileged. Non-root services need a D-Bus policy
  drop-in under `/etc/dbus-1/system.d/` allowing `org.freedesktop.Avahi.Server.EntryGroupNew`.
  Denial surfaces as `DBusException` with "Access denied" — never a silent no-op, so letting it
  propagate is safe and correct.
- **NetworkManager**: modifying connections is gated by polkit
  (`org.freedesktop.NetworkManager.settings.modify.system`). A denial is also a `DBusException`.
- **ModemManager**: similar, via polkit.

When a call fails with "Access denied", the fix is policy configuration on the device, not a
code change. Do not add retry loops or fallbacks around authorization failures.

---

## 7. Code conventions

- Modern C# where it improves clarity: file-scoped namespaces, collection expressions, primary
  constructors, pattern matching, `required` members.
- Every client is `IAsyncDisposable` and owns its `Connection`.
- Every I/O method takes a `CancellationToken` **and applies it** —
  `.WaitAsync(cancellationToken)` on the proxy task. Do not add a method that blocks without one.
- Validate arguments at the public boundary (`ArgumentNullException.ThrowIfNull`, explicit
  `ArgumentException` with `nameof`). The builders already do this; match the style.
- Public members on `*Client` classes get XML doc comments. `CS1591`/`CS1573` are suppressed
  repo-wide so proxy interfaces don't need per-parameter docs — that is not licence to leave the
  client classes undocumented.
- `.gitattributes` pins `.cs` to LF. Some tooling rewrites CRLF↔LF and turns a two-line change
  into a whole-file diff — if `git diff --stat` shows far more lines than you edited, that's what
  happened. Fix it before committing.

### The doc comments are the point

The proxy plumbing is mechanical. The value is the recorded behaviour of daemons that misbehave
in ways nobody documents. When you fix something that surprised you:

- Write down **what was observed**, not only what the code now does. "Restarting avahi-daemon
  drops the entry group while leaving the host name unchanged" is the useful sentence;
  "republish on failure" is not.
- Put it in the doc comment on the affected member so it survives refactoring, and mirror the
  user-facing consequence in README.md.
- Do not delete an explanatory comment because the code "looks obvious now". It looks obvious
  *because* the comment is there.
- Do not claim behaviour you inferred but did not observe. Mark it unverified, as §4 does.

---

## 8. Hard rules

- **No identifying references to any company, product, employer, internal hostname, device model,
  deployment path, or internal ticket ID.** This code was extracted from a private codebase and
  deliberately scrubbed. Examples in docs and comments use generic placeholders (`mydevice`,
  `_http._tcp`, `eth0`). Check anything you write, and anything you paste in from elsewhere.
- **No shared/common project between the three.** They are independent by design so a consumer
  can take one without the other two.
- **No dependency beyond `Tmds.DBus`** without a strong reason. Consumers are frequently
  embedded and trimmed; `IsTrimmable` is on and must stay satisfiable.
- **Proxy interfaces stay faithful to the daemon's introspection XML.** Put convenience on the
  `*Client` classes instead.
- **Treat the public surface as a contract.** These are published packages; follow semver and
  don't rename public types or namespaces casually.

---

## 9. Releasing

Version comes from the git tag via CI and is passed to `dotnet pack`; the local default is
`0.1.0-dev`. Tag `v0.2.0` → publishes `0.2.0` of all three packages.

The three version together even though they ship separately. That means occasional no-change
version bumps, in exchange for a trivial support matrix ("use 0.4.x of all three"). That trade
is deliberate — don't switch to independent versioning without a concrete reason.
