[![Windows CI](https://github.com/Vanderhell/IOBusMonitor/actions/workflows/windows-ci.yml/badge.svg)](https://github.com/Vanderhell/IOBusMonitor/actions/workflows/windows-ci.yml)
[![License: MIT](https://img.shields.io/github/license/Vanderhell/IOBusMonitor)](LICENSE)

# IOBusMonitor

Portable Windows desktop tool for **field-bus monitoring, short-term logging, and troubleshooting** of Modbus TCP, Modbus RTU, and Siemens S7 data.

> Status: active repair and usability work. The repository is usable, but the public documentation stays conservative and only describes implemented behavior.

## What It Is

IOBusMonitor helps service technicians, maintenance engineers, and system integrators:

- inspect live PLC/register values,
- log measurements into local SQLite archives,
- review recent history with built-in charts,
- configure devices, points, and measurements inside the app,
- evaluate the workflow without hardware by using demo mode.

It is designed for workstation use during commissioning, troubleshooting, and short-term observation of industrial signals.

## What It Is Not

- Not a SCADA replacement.
- Not a safety controller.
- Not a certified historian.
- Not a PLC programming environment.
- Not a write/control tool for PLC outputs unless a future feature explicitly implements and documents that behavior.

The current documented workflow is read-oriented monitoring and local logging.

## Target Users

- System integrators
- Maintenance engineers
- Service technicians
- Automation developers who need a quick diagnostic desktop tool

## Supported Protocols

| Protocol | Current use | Notes |
|---|---|---|
| Modbus TCP | Read-only polling | device, point, and measurement administration available in-app |
| Modbus RTU | Read-only polling | serial port settings and slave-based polling supported |
| Siemens S7 | Read-only polling | S7.Net-based read path with in-app device/point/measurement administration |

## Core Features

- Live dashboard for the latest values grouped from configured devices and points
- Local daily SQLite archive in `Data/Data_yyyyMMdd.db`
- History view with bounded loading and chart downsampling
- In-app administration for devices, points, and measurements
- Demo mode that seeds sample configuration and generates synthetic live values locally
- Portable packaging script for clean `Release|x64` ZIP creation

## Screenshots

PNG assets (1600x900) live in `docs/assets/screenshots/`:

- Home / Overview: [docs/assets/screenshots/home-overview.png](docs/assets/screenshots/home-overview.png)
- Dashboard: [docs/assets/screenshots/dashboard.png](docs/assets/screenshots/dashboard.png)
- History: [docs/assets/screenshots/history.png](docs/assets/screenshots/history.png)
- Settings: [docs/assets/screenshots/settings.png](docs/assets/screenshots/settings.png)
- Modbus TCP admin: [docs/assets/screenshots/modbus-tcp-admin.png](docs/assets/screenshots/modbus-tcp-admin.png)
- Modbus RTU admin: [docs/assets/screenshots/modbus-rtu-admin.png](docs/assets/screenshots/modbus-rtu-admin.png)
- Siemens S7 admin: [docs/assets/screenshots/siemens-s7-admin.png](docs/assets/screenshots/siemens-s7-admin.png)

## Quick Links

- Getting started: [docs/GETTING_STARTED.md](docs/GETTING_STARTED.md)
- Configuration guide: [docs/CONFIGURATION.md](docs/CONFIGURATION.md)
- Troubleshooting: [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)
- FAQ: [docs/FAQ.md](docs/FAQ.md)
- Build guide: [docs/BUILD.md](docs/BUILD.md)
- Release packaging: [docs/RELEASE.md](docs/RELEASE.md)
- Services: [docs/SERVICES.md](docs/SERVICES.md)
- Testing notes: [docs/TESTING.md](docs/TESTING.md)
- Contributing: [CONTRIBUTING.md](CONTRIBUTING.md)
- Security policy: [SECURITY.md](SECURITY.md)
- Support model: [SUPPORT.md](SUPPORT.md)

## Start Here

### Evaluate Without PLC Hardware

1. Build and launch the app on Windows.
2. On first run, let it create `Settings/Settings.db` and the default `Data` folder.
3. Click `Enable Demo Mode` from the shell prompt, or open `Settings` and enable `Demo mode`.
4. Click `Start Monitoring`.
5. Open the dashboard and history pages to inspect synthetic live data and archived samples.

### Build From Source

Windows prerequisites:

- Windows 10/11 x64
- Visual Studio 2019+ or Build Tools with `msbuild`
- NuGet CLI on `PATH`

```powershell
nuget restore .\IOBusMonitor.sln
msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64
```

See [docs/BUILD.md](docs/BUILD.md) for environment details and current WSL limitations.

### Create A Portable ZIP

```powershell
.\build\package-release.ps1
```

That script restores packages, builds `Release|x64`, stages a clean runtime folder, and creates a ZIP under `dist/`.

## Typical Workflow

1. Add a device for Modbus TCP, Modbus RTU, or Siemens S7.
2. Add one or more points under that device.
3. Add measurements under each point.
4. Use connection validation or test connection actions where available in the admin workflow.
5. Start monitoring.
6. Watch the latest values on the dashboard.
7. Open the history page to load recent archived samples.

## Runtime Layout

```text
IOBusMonitor.exe
Settings/Settings.db      # persistent app settings and configured devices/points/measurements
Data/Data_yyyyMMdd.db     # one SQLite archive per day
Logs/                     # optional diagnostic logs
```

## Current Limitations

- Windows desktop application only; this repository does not claim Linux or WSL runtime support for the WPF app.
- Documentation currently reflects read-only monitoring behavior. Output writing/control is not documented as supported.
- The app is intended for local monitoring and troubleshooting, not plant-wide supervisory control.
- Automated tests cover core library logic, not WPF rendering or real hardware communication.
- The README links to up-to-date screenshots under `docs/assets/screenshots/`.

## Demo Mode

- Demo mode is clearly marked in the shell with a `DEMO MODE` badge.
- While demo mode is enabled, the timer service does not poll real network or serial devices.
- Demo values are written into the normal SQLite archive path so dashboard and history workflows can be evaluated without hardware.
- `Reset Demo Sample Data` recreates demo configuration and clears previously generated demo archive rows for demo-named devices.

## Support Model

The codebase is MIT licensed and public. The repository is suitable for free issue-based collaboration and for paid custom integration or deployment work.

Use GitHub Issues for:

- reproducible bugs,
- build failures,
- documentation errors,
- feature proposals,
- protocol compatibility reports.

The repository has dedicated issue templates for bugs and feature requests under `.github/ISSUE_TEMPLATE/`.

For support boundaries and custom-work examples, see [SUPPORT.md](SUPPORT.md).

## Contact

- Bug reports and documentation issues: use [GitHub Issues](https://github.com/Vanderhell/IOBusMonitor/issues)
- Paid/custom work examples: see [docs/SERVICES.md](docs/SERVICES.md)
- Public paid-support email or website is not published in this repository at the moment

## Repository Presentation Notes

- CI badge reflects the real `windows-ci` GitHub Actions workflow in this repository.
- License badge reflects the repository license.
- No release badge is shown because a verified public release asset was not confirmed in this task.

## License

MIT. See [LICENSE](LICENSE).
