# WAVE — Wi-Fi Automated Validation Environment

Desktop tool for **automated Wi-Fi connectivity validation on Windows tablets**. It standardizes, speeds up and audits the network checks technicians run during acceptance testing, maintenance or field checklists, cutting human error by bundling several tests behind a single tap.

Built from the `especificacao_tecnica_wifi.pdf` specification and the `RegrasPrimordiaisDeDesenvolvimento.md` guidelines (SOLID, Clean Code, design patterns and security).

---

## What it does

The technician sees a screen of network buttons. Tapping a network makes WAVE:

1. Kill leftover browser/terminal instances (avoids memory buildup between tests).
2. Connect to the network — reusing the profile **Windows already has saved**, or creating one if needed.
3. Wait for a valid DHCP lease (up to 15s).
4. Fire three validations and show the results:
   - **Continuous ping** to `google.com` (visible window + live latency chart in-app);
   - **Speed test** on fast.com (private browser window);
   - **Streaming video** on YouTube (private window) to gauge stability.
5. Record the run in the history for auditing.

Each test moves through **IDLE → CONNECTING → TEST_RUNNING** (or **FAILED**), with color feedback: gray (idle), yellow (connecting), green (running) and red (failed). While a test runs, the other buttons are locked to prevent concurrent commands.

## Network discovery (no typing SSIDs)

On startup — and via the **Scan networks** button — WAVE scans visible networks (`netsh wlan show networks`) and merges them with the profiles already saved in Windows (`netsh wlan show profiles`). Each network becomes a button with a status line (security, readiness and signal):

- **Open** or **already known to Windows** → shown as "ready" and connects **without a password**.
- **Protected but still unknown** → shown as "needs password"; an administrator enters the password once. After that Windows knows the network and the operator tests it directly.

So manual registration is the exception, not the rule.

## Access roles (RBAC) and the admin password

Because managing network credentials is a sensitive operation, the app uses role-based access control, enforced in the application layer (not just the UI):

- **Operator** (default): runs tests and views history.
- **Administrator**: also registers/edits networks and changes settings.

The **admin password** (the *Elevate access* field) protects the app itself and is **not** a Wi-Fi password. On first run, the first password typed becomes the admin password (stored as a **PBKDF2-SHA256** hash, never in plain text). **Wi-Fi passwords** are stored separately, encrypted with **DPAPI**.

## Telemetry and auditing

- **Live latency**: a chart of latency (ms) plus last / average / packet-loss indicators, computed from a background ping.
- **History**: each run records network, timestamp, result (success/failure and reason) and ping statistics for auditing.

## Architecture

Layered solution (Clean Architecture), with dependencies always pointing inward:

```
src/
  WAVE.Domain          # Pure core: models, enums, Result (no dependencies)
  WAVE.Application     # Abstractions, RBAC, discovery and orchestrator (state machine)
  WAVE.Infrastructure  # Windows: netsh, processes, ping, browser, DPAPI, JSON
  WAVE.App             # WPF front end (MVVM), reusable components, DI composition
tests/
  WAVE.UnitTests       # Pure-logic tests (run on any OS)
docs/
  ARQUITETURA.md       # Layer, pattern and spec-mapping details (in Portuguese)
```

Back end and front end are separated; the front end is componentized (network button, latency chart, responsive portrait/landscape layout, add-network panel). Patterns applied without overengineering: MVVM, State, Strategy, Repository, Factory, Dependency Injection, Observer and Result. Details in [`docs/ARQUITETURA.md`](docs/ARQUITETURA.md).

## Requirements

- **Windows 10 or 11** (x64 or ARM64). Automations use native Windows APIs.
- **.NET 8 SDK** only to build. The published `.exe` is self-contained and **does not require .NET installed** on the target machine.

## Build, test and run

```powershell
dotnet build
dotnet test
dotnet run --project src/WAVE.App
```

## Build the single-file `.exe` (self-contained)

```powershell
./publish.ps1
```

This produces `publish/win-x64/WAVE.exe` and `publish/win-arm64/WAVE.exe` — each a **single file, no installer**, that runs on any Windows machine of the matching type. Or manually:

```powershell
dotnet publish src/WAVE.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

## First run

1. Open `WAVE.exe`. Nearby networks show up automatically (or click **Scan networks**).
2. Tap an **open** or **already known** network to test — no password needed.
3. For a protected, still-unknown network: type a password (≥8 chars) at the top and click **Elevate access** (on first run this becomes the admin password), register the network in the form, then test.

## Where data lives / security

Local data lives in `%LOCALAPPDATA%\WAVE`: profiles, history, **encrypted** credentials and logs. Security summary: RBAC enforced in the application layer, Wi-Fi credentials under DPAPI, admin password under PBKDF2, input validation and command-line argument sanitization.

## Known limitations / next steps

- **Enterprise (802.1X)** networks are not yet supported by the profile generator (Personal and Open are).
- **Speed (fast.com)** and **streaming (YouTube)** run in browser windows; capturing those numbers automatically in-app is a next step (today the history records ping telemetry).
- Process termination between runs is by **name** (`cmd`, `msedge`, `chrome`), per the spec — it may close other windows of those processes. The list is configurable in `TestRunnerOptions`.
- The test video URL is neutral and configurable (avoids hardcoding an example).
