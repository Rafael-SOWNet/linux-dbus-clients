# AGENTS.md

Guidance for AI coding agents (and humans skimming for conventions) working in this repository.

## What this repository is

Three independent .NET client libraries over Linux system daemons, published as three separate
NuGet packages from one repository. They share build configuration and CI, and nothing else —
there is deliberately no common "core" package.

| Directory | Namespace / assembly | Package ID |
|---|---|---|
| `src/AvahiDbus/` | `AvahiDbus` | `Dbus.Clients.Avahi` |
| `src/NetworkManagerDbus/` | `NetworkManagerDbus` | `Dbus.Clients.NetworkManager` |
| `src/ModemManagerDbus/` | `ModemManagerDbus` | `Dbus.Clients.ModemManager` |

The package ID intentionally differs from the assembly name: the assembly names are short and
readable in stack traces, while the package IDs are namespaced enough to be unambiguous on
nuget.org. Do not "fix" this by aligning them.

## Build and verify

```bash
dotnet build -c Release          # must be 0 errors, 0 warnings
dotnet pack -c Release -o ./artifacts
```

There is no test project yet. Meaningful tests need a live D-Bus system bus and the daemons
themselves, so they belong in a container-based integration suite rather than as unit tests
over the proxy interfaces — mocking `Tmds.DBus` proxies would only test the mock. If you add
tests, add the harness, not the mocks.

Because there are no tests, **a build alone does not tell you a change is correct.** Any change
to daemon-interaction logic needs to be exercised against a real daemon, or clearly flagged as
unverified in the PR description.

## Layout conventions

Within each project:

- `Dbus/Proxies.cs` — `[DBusInterface]` interfaces mirroring the daemon's introspection XML.
  Keep these faithful to the published interface. No convenience methods, no reinterpretation
  of values, no merging of two D-Bus interfaces into one C# interface.
- `Clients/*Client.cs` — the hand-written, ergonomic surface. All convenience lives here.
- `Dto/*.cs` — typed request/response models replacing D-Bus's
  `Dictionary<string, Dictionary<string, object>>` settings dictionaries.
- `Builders/`, `Mappers/` — translation between the DTOs and the settings dictionaries.
- `*Constants.cs`, `*Enums.cs`, `*Types.cs` — daemon constants and enumerations, named to match
  the upstream C names so they can be grepped against the daemon's own documentation.

`ModemManagerDbus` is flat rather than layered, because it is overwhelmingly generated-shape
per-interface clients. Do not restructure it for symmetry alone.

## Code conventions

- Target `net10.0`, `LangVersion=preview`, nullable enabled. Use modern C# — file-scoped
  namespaces, collection expressions, primary constructors, pattern matching — where it improves
  clarity.
- Every client is `IAsyncDisposable` and owns its `Connection`. Preserve that: leaking a D-Bus
  connection per retry is a real failure mode, not a theoretical one.
- All I/O methods take a `CancellationToken` and apply it (`.WaitAsync(cancellationToken)` on
  the proxy task). Do not add a method that blocks without one.
- Public members get XML doc comments. `CS1591`/`CS1573` are suppressed repo-wide so that the
  proxy interfaces don't need per-parameter docs — that suppression is not licence to leave the
  `*Client` classes undocumented.

## The doc comments are the point

This library's real value is not the proxy plumbing — that is mechanical. It is the recorded
behaviour of daemons that misbehave in ways nobody documents: avahi renaming your host on
collision, entry groups vanishing across a daemon restart, states that arrive out of order.

So, when you fix something that surprised you:

- Write down **what was observed**, not only what the code now does. "Restarting avahi-daemon
  drops the entry group while leaving the host name unchanged" is the useful sentence; "republish
  on failure" is not.
- Put it in the doc comment on the member it affects, so it survives refactoring, and mirror the
  user-facing consequence in README.md's "Things that will bite you".
- Do not delete an explanatory comment because the code "looks obvious now". It looks obvious
  because the comment is there.

## Absolutely do not

- **Add identifying references to any specific company, product, employer, internal hostname,
  device model, deployment path, or internal ticket ID.** This code was extracted from a private
  codebase and deliberately scrubbed of all of it. Examples in docs and comments must use
  generic placeholders (`mydevice`, `_http._tcp`, `eth0`). This is a hard rule: check any
  comment you write, and any comment you copy in from elsewhere.
- Introduce a shared/common project between the three. They are independent by design so a
  consumer can take one without the other two.
- Add a dependency beyond `Tmds.DBus` without a strong reason. Consumers are frequently
  embedded/trimmed builds; `IsTrimmable` is on and should stay satisfiable.
- Rename public types or namespaces casually. These are published packages with external
  consumers; treat the public surface as a contract and follow semver.

## Releasing

Versions come from the git tag via CI (`Version` is passed to `dotnet pack`); the local default
is `0.1.0-dev`. Tag `v0.2.0` to publish `0.2.0` of all three packages — they version together
even though they ship separately, which keeps the support matrix trivial at the cost of some
no-change version bumps. That trade is deliberate.
