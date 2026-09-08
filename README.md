# AeterniUI

AeterniUI is a Blazor component library designed to provide a consistent UI across Blazor Server, Blazor WebAssembly, and Tauri + Blazor WebAssembly applications.

## Requirements

- .NET SDK 10.0 or a compatible .NET 10 SDK.
- Rust toolchain.
- Tauri CLI 2.x for desktop development.
- A platform WebView supported by Tauri.

The project uses .NET 10 packages and targets `net10.0`. Restore NuGet and Cargo dependencies on the new machine instead of copying generated output from the old machine.

## Projects

- `src/AeterniUI`: component library.
- `src/AeterniUI.Icons.FontAwesome`: optional Font Awesome icon definitions.
- `src/AeterniUI.Sample`: Blazor WebAssembly sample.
- `tauri/src-tauri`: Tauri desktop host.
- `docs/component-design-guidelines.zh-CN.md`: component development rules.
- `docs/current-features.zh-CN.md`: current implemented feature list.

## Start the Sample in Tauri

```bash
cd tauri/src-tauri
cargo tauri dev
```

This command starts the sample through the configured `beforeDevCommand` and enables `dotnet watch` hot reload. Do not run a second `dotnet watch` on port `5178`.

If startup reports `address already in use`, find the process and stop the previous development host:

```bash
lsof -nP -iTCP:5178 -sTCP:LISTEN
kill <PID>
```

If static web asset paths contain `obj\\Debug`, stop all old watchers before cleaning generated output. `bin`, `obj`, and `target` should not be copied between computers.

## Use the Library

Register the services in the application:

```csharp
builder.Services.AddAeterniUI();
```

Place the providers once in the root layout:

```razor
<ThemeProvider />
<DialogProvider />
@Body
```

The public API and component rules are documented in the files under `docs/`.

## Move to Another Computer

1. Copy or clone the source tree, excluding generated output and IDE metadata.
2. Install the required .NET SDK, Rust toolchain, and Tauri CLI.
3. Run `dotnet restore aeterni_ui.slnx`.
4. Run `cargo fetch --manifest-path tauri/src-tauri/Cargo.toml`.
5. Start with `cargo tauri dev` from `tauri/src-tauri`.
6. Open `AGENTS.md` before asking another coding agent to modify the project.

Machine-specific proxy settings belong in the new computer's shell or Agent configuration, not in the repository. Use the correctly named `HTTPS_PROXY` variable when configuring an HTTPS proxy.
