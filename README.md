# Zerotier GUI - Frontend Direction

This repository hosts a modern Zerotier tray UI for Linux Mint. The app is written in .NET (Avalonia), with first-class support for Mint Cinnamon and follow-on support for XFCE and MATE. Broader cross-platform considerations are deprioritized for now.

## Prioritized direction
1. **Mint Cinnamon first**: Optimize visuals and tray behavior for Cinnamon defaults.
2. **Extend to XFCE and MATE**: Validate tray/menu handling on these desktops using the same approach.
3. **Cross-platform (later)**: Consider other desktops or OSes only after the Mint variants are solid.

## Recommended stack
- **Avalonia UI (C#)**: Modern XAML-based UI that fits the desired clean aesthetic and works well on Linux. Avalonia targets Wayland/X11 and packages cleanly for Mint.
- **Tray icon/menu**: Use the Avalonia TrayIcon API with **Ayatana AppIndicator** bindings to ensure consistent tray behavior across Cinnamon, MATE, and XFCE. AppIndicator is the de-facto tray protocol on Mint and remains compatible with GNOME derivatives.
- **Process split**: Keep Zerotier CLI interactions in a background service (e.g., hosted worker) and expose async IPC to the tray UI to avoid UI hangs when calling the CLI.

## Project layout
- `src/Zerotier.Gui/` – Avalonia desktop app targeting .NET 8 with a starter window, view models, and tray icon wiring.

## Tray-enabled starter app
The Avalonia app is wired with a tray icon and native menu using `TrayIcon` and `NativeMenu`. Selecting **Open** reactivates the main window, and **Quit** exits the app. The tray icon defaults to the AppIndicator provider on Linux when the desktop supports it. Bring your own indicator artwork by adding an icon (e.g., `tray.png` or `.ico`) under `src/Zerotier.Gui/Assets/` and referencing it from `App.axaml`.

## Getting started
1. Install the .NET 8 SDK on your development machine.
2. From the repo root, restore and run the desktop app:
   ```bash
   dotnet restore
   dotnet run --project src/Zerotier.Gui
   ```
3. On Mint/XFCE/MATE, ensure the `ayatana-appindicator` package (or equivalent) is installed so the tray icon renders reliably.

### Dev machine prerequisites (Ubuntu 24.04 example)
If your dev container is based on Ubuntu 24.04 (as used here), add Microsoft's package feed and install the SDK:

```bash
sudo apt-get update
sudo apt-get install -y wget apt-transport-https
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
dotnet --info
```

For runtime-only scenarios, `dotnet-runtime-8.0` is sufficient, but the SDK is recommended for building and testing.

## Implementation notes
- Prefer AppIndicator/Ayatana tray integration over legacy `GtkStatusIcon` to avoid issues on modern desktops.
- Target .NET 8 for long-term support and better native packaging options.
- Plan for distribution via `.deb` with dependencies on `ayatana-appindicator` (or equivalent) and the Zerotier CLI.
- Use asynchronous command execution (e.g., `Process` with cancellation) and thread-safe UI updates to keep the tray responsive.
- Maintain an in-memory network cache refreshed every ~30 seconds to avoid repeated CLI calls on each click; marshal updates back to the UI thread.
- Maintain a small preferences window in Avalonia for network list, connect/disconnect, and status indicators; keep heavy operations off the UI thread.

## TODO / next steps
These items are scoped for Mint Cinnamon first, then validated on XFCE and MATE:
- Wire a background Zerotier CLI service with async IPC to the tray/UI.
- Display current network list and connection state in the main window and tray menu (tray sample now seeded with mock networks).
- Refresh cached network state on a 30–60s interval and reuse it for tray/menu clicks instead of calling the CLI each time.
- Add connect/disconnect actions per network with inline status feedback and toggle defaults/managed/global routes from the tray.
- Provide a compact preferences sheet (start on login toggle, managed controller URL, identity path selector).
- Implement error notifications (e.g., AppIndicator menu badges or toast) for CLI failures or auth issues.
- Package a `.deb` that depends on `ayatana-appindicator` and the Zerotier CLI; document install/test steps for Mint.

## Zerotier CLI command map (GUI integration guide)
These are the core `zerotier-cli` commands and settings the GUI should surface. Commands are ordered by priority for Mint
Cinnamon, then XFCE and MATE. Use `-j` to request JSON output for easier parsing where supported.

### Daemon and identity
- `status` – quick daemon state check (OK/PORTERROR/ONLINE/OFFLINE/etc.).
- `info` – node ID, address, status, online/offline, and version.

### Networks
- `listnetworks [-j]` – list joined networks with status fields (status, type, name, IP assignments, routes).
- `join <networkId>` – join a network.
- `leave <networkId>` – leave a network (stop using its routes/addresses).
- `get <networkId> <key>` – read a network setting (e.g., `allowDefault`, `allowManaged`, `allowGlobal`, `portError`).
- `set <networkId> <key> <value>` – change a network setting. Common keys the GUI should expose:
  - `allowDefault` (`1`/`0`): accept managed default route (enables/disables default routes from the controller).
  - `allowManaged` (`1`/`0`): allow controller-managed routes and DNS.
  - `allowGlobal` (`1`/`0`): allow IPv6 global routes.
  - `portError` (`1`/`0`): toggle UDP port error reporting.

### Peers and moons
- `listpeers [-j]` – show peers (address, latency, version, role, status).
- `listmoons [-j]` – show moons (managed roots) the node orbits.
- `orbit <worldId> <seed>` – add a moon.
- `deorbit <worldId>` – remove a moon.

### Diagnostics and utilities
- `listcontrollers [-j]` – list controllers the node knows about.
- `dump` – dump detailed state (only use for debugging; not UI-facing by default).
- `multicast [limit]` – show multicast group membership.
- `help` – built-in CLI help for completeness.

### Notes for the GUI
- Prefer `-j` variants when available to avoid parsing text output.
- Avoid long-running calls on the UI thread; wrap CLI execution with cancellation and timeouts.
- Persist per-network toggles (like `allowDefault`) in the UI and refresh after `set` to confirm the daemon accepted the value.
