# WAVE — Wi-Fi Automated Validation Environment

Desktop tool for **automated Wi-Fi connectivity validation on Windows tablets**. It standardizes, speeds up and audits the network checks technicians run during acceptance testing, maintenance or field checklists, cutting human error by bundling several tests behind a single tap.

**Download:** [WAVE.exe (Windows x64)](https://github.com/FredericoTatagiba/WAVE/releases/latest/download/WAVE.exe) — single file, no installer. Just download and run.

Built following SOLID and Clean Code principles, with design patterns and security applied where they add value.

---

## What it does

The technician opens the app onto a screen of network buttons — plus one for the cable. Tapping a target makes WAVE:

1. Connect to the network — reusing the profile **the system already has saved**, or creating one if needed.
2. Wait for a valid DHCP lease (up to 15s).
3. Fire three validations and show the results:
   - **Continuous ping** to `google.com`, plotted live in the app;
   - **Speed test**: download/upload throughput over HTTP;
   - **Streaming probe**: sustained bitrate, to gauge stability.
4. Record the run in the history for auditing (including which user ran it).

Everything runs inside the app — no terminal or browser windows are opened.

Each test moves through **IDLE → CONNECTING → TEST_RUNNING** (or **FAILED**), with color feedback: gray (idle), yellow (connecting), green (running) and red (failed). Connecting is the one phase with no telemetry of its own, so it also shows a progress indicator on the network button and in the status bar. While a test runs, the other buttons are locked to prevent concurrent commands.

## Network discovery (no typing SSIDs)

On startup — and via the **Scan networks** button — WAVE scans visible networks (`netsh wlan show networks`) and merges them with the profiles already saved in Windows (`netsh wlan show profiles`). Each network becomes a button with a status line (security, readiness and signal):

- **Open** or **already known to Windows** → shown as "ready" and connects **without a password**.
- **Protected but still unknown** → shown as "needs password"; the technician enters the passphrase once, and WAVE only remembers it after the connection actually succeeds — a wrong password is never stored. After that the system knows the network and it is tested directly.

So manual registration is the exception, not the rule.

## No sign-in, one administrator password

**There is no login screen.** WAVE opens straight into the network list, because running a test and reading the history need no identity. The password only appears when someone reaches for an administrator action:

- **Registering or deleting a network** in the catalog (which stores its Wi‑Fi password).
- **Changing the settings** — where the history and the logs are written.

The first time either is attempted, WAVE asks you to **create** the administrator password (min. 8 characters, stored as a **PBKDF2‑SHA256 hash**); after that it asks you to enter it, and the session stays unlocked until the app closes.

Everything else — scanning, testing Wi‑Fi and cable, viewing and exporting the history, and remembering the passphrase of a network the technician just connected to — needs nothing.

> **Why not per-technician logins?** On a shared field tablet, a typed password makes accounts converge into one shared account, and then the "who ran this test" column names someone nobody can vouch for — worse in an audit than no name at all. WAVE records the **device** instead, which is a fact it can actually assert. If a per-person audit trail is a real requirement, the answer is shipping results off the device, not a login screen: the history is a local JSON file that anyone with filesystem access can edit, so a local login would only be decoration.

> **Lost the administrator password?** Delete the `adminPasswordHash` entry from `%LOCALAPPDATA%\WAVE\settings.json` (or the whole file, which also resets the paths). WAVE will ask you to create the password again on the next administrator action. Note this makes the password gate an application-level control, not a barrier against someone with access to the device's filesystem — the same has always been true of the Wi‑Fi credentials, which DPAPI binds to the Windows account rather than to anything WAVE asks for.

## Telemetry and auditing

- **Live latency**: a chart of latency (ms) plus last / average / packet-loss indicators, computed from a background ping.
- **Steadiness, not just speed**: **jitter** (mean variation between consecutive replies) and the **95th percentile**. A link parked at 20 ms and one swinging between 5 and 35 ms share an average; only the second one stutters, and only these two numbers tell them apart.
- **Bufferbloat**: latency is measured twice — with the link at rest, then again while the throughput test saturates it. The gap between the two ("18 → 240 ms sob carga") is what decides whether a call or a game survives someone starting a download on the same link. An average that blends both phases describes neither.
- **Pick what you are pinging**: the target sits next to the indicators it explains and can be changed per test — a raw IP takes DNS out of the path, and the gateway separates "my link" from "upstream". Every run records the target it used, so two rows of the history are never compared as if they measured the same thing when they did not. The device default lives in **Configurações**.
- **History**: each run records network, medium (Wi‑Fi or cable), timestamp, the device it ran on, the ping target, result (success/failure and reason) and the full ping statistics.

- **Where it is written**: by default `%LOCALAPPDATA%\WAVE`, but the history and log directories are configurable — point them at a share and the reports collect themselves. An unreachable target degrades to the local folder instead of losing the run.

> These are ICMP measurements against a fixed host, sampled every 200 ms. They characterise the link, not a specific application: routers treat ICMP on a separate, often rate-limited path, and the route to `google.com` is not the route to a game or VoIP server.

## Architecture

Layered solution (Clean Architecture), with dependencies always pointing inward:

```
src/
  WAVE.Domain          # Pure core: models, enums, Result (no dependencies)
  WAVE.Application     # Abstractions, admin gate, settings, discovery and orchestrator (state machine)
  WAVE.Infrastructure  # netsh/nmcli, processes, ping, DPAPI/AES, PBKDF2, JSON
  WAVE.App             # Avalonia front end (MVVM), reusable components, DI composition
tests/
  WAVE.UnitTests       # Pure-logic tests
docs/
  ARQUITETURA.md       # Layer, pattern and spec-mapping details (in Portuguese)
```

Back end and front end are separated; the front end is componentized (network button, latency chart, responsive portrait/landscape layout, add-network panel, settings and administrator-password windows). Patterns applied without overengineering: MVVM, State, Strategy, Repository, Factory, Dependency Injection, Observer and Result. Details in [`docs/ARQUITETURA.md`](docs/ARQUITETURA.md).

## Requirements

Runs on **Windows 10/11** (x64 or ARM64) and on **Linux** (x64). The build is self-contained — no .NET install needed on the machine that runs it.

On Linux it additionally needs:

- **NetworkManager** (`nmcli`) — WAVE drives Wi-Fi through it, the way it uses `netsh` on Windows.
- `libX11`, `libSM`, `libICE`, `libfontconfig1` for the UI toolkit.
- Permission to change network settings. Changing Wi-Fi settings is polkit-protected: on a desktop session you are prompted for a password each time, and with no polkit agent (over SSH, on a kiosk) the operation fails outright. To grant it once:

  ```bash
  sudo cp packaging/49-wave-nmcli.rules /etc/polkit-1/rules.d/
  sudo usermod -aG netdev "$USER"   # log out and back in
  ```

## The executable

Download the latest build from the **[Releases](https://github.com/FredericoTatagiba/WAVE/releases)** page — the single-file, self-contained build is too large to live in the repository. Locally, a build lands at `publish/win-x64/WAVE.exe`, `publish/win-arm64/WAVE.exe` or `publish/linux-x64/WAVE`, produced by `publish.ps1` (Windows) or `publish.sh` (Linux).

## First run

1. Open WAVE. You land straight on the main screen — there is no sign-in. Nearby networks show up automatically (or click **Scan networks**), and the **Cabo de rede** button sits above them.
2. Tap a network — or the cable — to test. **Open** and **already known** networks need no password.
3. For a protected, still-unknown network, WAVE asks for its passphrase once and remembers it after the connection actually succeeds.
4. **Configurações** (top bar) and registering a network in the catalog ask for the administrator password, creating it on first use.

## Where data lives / security

Local data lives in `%LOCALAPPDATA%\WAVE` on Windows and `~/.local/share/WAVE` on Linux: settings (with the PBKDF2 administrator hash), network profiles, **encrypted** Wi‑Fi credentials, plus the history and logs — the last two relocatable from **Configurações**. Security summary: administrator actions gated in the application layer, the password under PBKDF2, input validation, and network-tool arguments passed as an argument vector (never through a shell).

Wi‑Fi credentials are encrypted at rest with **DPAPI** on Windows and with **AES-GCM** on Linux, keyed by a random 256-bit key in a `0600` file inside the `0700` data directory. The two formats are not interchangeable: a data directory copied between machines, OS accounts or operating systems will not decrypt, and WAVE simply asks for the passphrase again.

## Known limitations / next steps

- **Enterprise (802.1X)** networks are supported via PEAP-MSCHAPv2 (user/password, optional logon domain). On Windows the credentials are applied to the profile through the native WLAN API; on Linux they go straight into the NetworkManager connection. Other EAP methods (TLS/certificates) are a next step.
- **macOS** is not supported. Scanning without elevation has no stable command-line path since `airport` was removed in Sonoma 14.4, so it would need a native CoreWLAN binding.
- **Speed and streaming** are now measured in-app via HTTP: download/upload throughput (Mbps) and a sustained-bitrate streaming stability verdict, both recorded in the history alongside ping telemetry (no browser windows). Endpoints and the target bitrate are configurable in `TestRunnerOptions`.
- WAVE launches **no external processes** for a test: ping, throughput and streaming are all measured in-process. Nothing pops up over the technician's desktop, and there is nothing to clean up between runs.
- The test video URL is neutral and configurable (avoids hardcoding an example).
