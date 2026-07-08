# WAVE — Wi-Fi Automated Validation Environment

Desktop tool for **automated Wi-Fi connectivity validation on Windows tablets**. It standardizes, speeds up and audits the network checks technicians run during acceptance testing, maintenance or field checklists, cutting human error by bundling several tests behind a single tap.

**Download:** [WAVE.exe (Windows x64)](https://github.com/FredericoTatagiba/WAVE/releases/latest/download/WAVE.exe) — single file, no installer. Just download and run.

Built following SOLID and Clean Code principles, with design patterns and security applied where they add value.

---

## What it does

The technician signs in, then sees a screen of network buttons. Tapping a network makes WAVE:

1. Kill leftover browser/terminal instances (avoids memory buildup between tests).
2. Connect to the network — reusing the profile **Windows already has saved**, or creating one if needed.
3. Wait for a valid DHCP lease (up to 15s).
4. Fire three validations and show the results:
   - **Continuous ping** to `google.com` (visible window + live latency chart in-app);
   - **Speed test** on fast.com (private browser window);
   - **Streaming video** on YouTube (private window) to gauge stability.
5. Record the run in the history for auditing (including which user ran it).

Each test moves through **IDLE → CONNECTING → TEST_RUNNING** (or **FAILED**), with color feedback: gray (idle), yellow (connecting), green (running) and red (failed). While a test runs, the other buttons are locked to prevent concurrent commands.

## Network discovery (no typing SSIDs)

On startup — and via the **Scan networks** button — WAVE scans visible networks (`netsh wlan show networks`) and merges them with the profiles already saved in Windows (`netsh wlan show profiles`). Each network becomes a button with a status line (security, readiness and signal):

- **Open** or **already known to Windows** → shown as "ready" and connects **without a password**.
- **Protected but still unknown** → shown as "needs password"; an administrator enters the password once. After that Windows knows the network and the operator tests it directly.

So manual registration is the exception, not the rule.

## Users, roles and the administrator (RBAC)

WAVE requires a **login**, and access is controlled by role (enforced in the application layer, not just the UI):

- **Operator**: runs tests and views history.
- **Administrator**: everything an operator does, plus registers/edits networks and their Wi‑Fi passwords, manages users, and changes settings.

### First-time setup: creating the administrator

The **very first time** WAVE runs there are no users, so it opens a **"Create administrator"** screen. You define the administrator's **login, display name and password** (min. 8 characters). This first account is the anchor of the whole access model:

- It is the account that can **register the Wi‑Fi networks and their passwords** used for the tests.
- It can **create the other users** (the technicians who will run the tests) and assign each one a role.
- It can **reset passwords, change roles and remove users**, and change settings.

Without this administrator, there is no way to configure networks or add people — that is exactly why WAVE forces its creation up front. The password is stored as a **PBKDF2‑SHA256 hash** (never in plain text). WAVE protects the **last administrator** from being deleted or demoted, so you can never lock yourself out.

### Day-to-day

After setup, WAVE opens the **login** screen; sign in with your account. In the top bar, **Usuários** (admin only) opens user management to add technicians (usually as Operators), reset passwords, change roles or remove users; **Sair** switches user. The history records **who** ran each test. Wi‑Fi passwords are stored separately, encrypted with **DPAPI**.

> Lost the admin password? Delete `%LOCALAPPDATA%\WAVE\users.json` and WAVE will ask you to create the administrator again on the next launch.

## Telemetry and auditing

- **Live latency**: a chart of latency (ms) plus last / average / packet-loss indicators, computed from a background ping.
- **History**: each run records network, timestamp, the user who ran it, result (success/failure and reason) and ping statistics.

## Architecture

Layered solution (Clean Architecture), with dependencies always pointing inward:

```
src/
  WAVE.Domain          # Pure core: models, enums, Result (no dependencies)
  WAVE.Application     # Abstractions, RBAC, auth, discovery and orchestrator (state machine)
  WAVE.Infrastructure  # Windows: netsh, processes, ping, browser, DPAPI, PBKDF2, JSON
  WAVE.App             # WPF front end (MVVM), reusable components, DI composition
tests/
  WAVE.UnitTests       # Pure-logic tests
docs/
  ARQUITETURA.md       # Layer, pattern and spec-mapping details (in Portuguese)
```

Back end and front end are separated; the front end is componentized (network button, latency chart, responsive portrait/landscape layout, add-network panel, login and user-management windows). Patterns applied without overengineering: MVVM, State, Strategy, Repository, Factory, Dependency Injection, Observer and Result. Details in [`docs/ARQUITETURA.md`](docs/ARQUITETURA.md).

## Requirements

**Windows 10 or 11** (x64 or ARM64). The `.exe` is self-contained — no .NET install needed on the machine that runs it.

## The executable

Download the latest **WAVE.exe** from the **[Releases](https://github.com/FredericoTatagiba/WAVE/releases)** page — the single-file, self-contained build is too large to live in the repository. Locally, a build lands at `publish/win-x64/WAVE.exe` (and `publish/win-arm64/WAVE.exe` for ARM devices).

## First run

1. Open `WAVE.exe`. The first time, the **Create administrator** screen appears — set the admin login, display name and password (≥8 chars). This account administers the app.
2. You land on the main screen; nearby networks show up automatically (or click **Scan networks**).
3. Tap an **open** or **already known** network to test — no password needed. For a protected, still-unknown network, an administrator registers the Wi‑Fi password once.
4. Use **Usuários** (admin) to add technicians as Operators; **Sair** switches user.

## Where data lives / security

Local data lives in `%LOCALAPPDATA%\WAVE`: users (with PBKDF2 password hashes), network profiles, history, **encrypted** Wi‑Fi credentials and logs. Security summary: login required, RBAC enforced in the application layer, Wi‑Fi credentials under DPAPI, passwords under PBKDF2, input validation and command-line argument sanitization.

## Known limitations / next steps

- **Enterprise (802.1X)** networks are supported via PEAP-MSCHAPv2 (user/password, optional logon domain); the credentials are applied to the profile through the native WLAN API. Other EAP methods (TLS/certificates) are a next step.
- **Speed and streaming** are now measured in-app via HTTP: download/upload throughput (Mbps) and a sustained-bitrate streaming stability verdict, both recorded in the history alongside ping telemetry (no browser windows). Endpoints and the target bitrate are configurable in `TestRunnerOptions`.
- Process termination between runs is **scoped by PID**: WAVE only closes the processes it launched itself (today, the visible ping window), tracked by process id — it never kills the technician's own browsers or terminals.
- The test video URL is neutral and configurable (avoids hardcoding an example).
