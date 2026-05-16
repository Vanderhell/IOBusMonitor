# IOBusMonitor

Portable Windows desktop tool for **field-bus monitoring, short-term logging and troubleshooting** of Modbus TCP, Modbus RTU and Siemens S7 data.

> Status: active reform / technician-grade usability work in progress.

## What it is

IOBusMonitor helps service technicians and system integrators quickly inspect PLC/register values, log them to local SQLite archives, view live values and open simple history charts.

It is designed for workstation use during commissioning, troubleshooting and short-term process observation.

## What it is not

- Not a SCADA replacement.
- Not a safety controller.
- Not a certified historian.
- Not a PLC programming tool.
- Not intended to write/control PLC outputs unless a future feature explicitly implements and documents that.

## Supported protocols

| Protocol | Status | Notes |
|---|---:|---|
| Modbus TCP | supported | read holding registers |
| Modbus RTU | supported | serial Modbus polling |
| Siemens S7 | supported/in progress | S7.Net based read path |

## Core features

- Live dashboard for configured devices/points/measurements.
- Local daily SQLite archive: `Data/Data_yyyyMMdd.db`.
- History charts from archived data.
- In-app configuration for devices, points and measurements.
- Portable deployment: copy release folder and run executable.
- Demo mode planned/available depending on release.

## Target users

- System integrators.
- Maintenance engineers.
- Service technicians.
- Automation developers who need a quick field diagnostic tool.

## Quick start from source

Requirements:

- Windows 10/11 x64.
- Visual Studio 2019 or newer with .NET Framework desktop workload.
- NuGet package restore enabled.

```powershell
nuget restore .\IOBusMonitor.sln
msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64
```

Run:

```powershell
.\IOBusMonitorind\Release\IOBusMonitor.exe
```

## Runtime folders

```text
Settings/Settings.db      # app configuration and device definitions
Data/Data_yyyyMMdd.db     # daily measurement archive
Logs/                     # diagnostic logs
```

## Typical workflow

1. Add a device.
2. Add a point under the device.
3. Add measurements/registers/addresses under the point.
4. Test connection where supported.
5. Start monitoring.
6. Watch live values.
7. Open chart/history.
8. Export or inspect SQLite archive.

## Screenshots

Replace these after the WPF shell/dashboard redesign:

- Home
- Live dashboard
- History chart
- Device configuration

## Support and custom work

The core project is MIT licensed. Paid support/custom work can cover:

- device-specific templates,
- protocol adapters,
- deployment packaging,
- branded internal builds,
- CSV/report exports,
- technician training,
- troubleshooting and bug-fix priority.

See `SUPPORT.md` and `docs/SERVICES.md`.

## License

MIT — see `LICENSE`.
