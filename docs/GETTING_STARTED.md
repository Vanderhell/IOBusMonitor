# Getting Started

## Who This Guide Is For

This guide is for technicians and integrators who want to:

- evaluate the application without PLC hardware,
- connect a first Modbus or Siemens device,
- understand where configuration and archive data are stored.

## Prerequisites

- Windows 10/11 x64
- Visual Studio 2019+ or Windows build tools if building from source
- NuGet CLI and `msbuild` on `PATH` for source builds

## Option 1: Evaluate With Demo Mode

1. Build and start `IOBusMonitor`.
2. On first launch, let the app create `Settings/Settings.db`.
3. When the shell prompt appears, click `Enable Demo Mode`.
4. Click `Start Monitoring`.
5. Open `Dashboard` to see synthetic live values.
6. Open `History` to view archived demo samples.

What demo mode does:

- seeds sample Modbus TCP, Modbus RTU, and Siemens S7 configuration,
- marks the shell with a `DEMO MODE` badge,
- generates synthetic values locally through the normal archive path,
- avoids real serial or network polling while enabled.

## Option 2: Connect A Real Device

1. Open the relevant settings page for the protocol.
2. Add a device.
3. Add a point under that device.
4. Add one or more measurements.
5. Save the changes.
6. Use test connection or validation actions where the page exposes them.
7. Click `Start Monitoring`.

## Build From Source

```powershell
nuget restore .\IOBusMonitor.sln
msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64
```

The WPF application must be built from Windows. Current repository docs do not claim WSL build support for the app.

## Create A Portable Package

```powershell
.\build\package-release.ps1
```

The script creates a clean ZIP in `dist/` and excludes local `Settings`, `Data`, and `Logs` by default.

## Runtime Files

```text
Settings/Settings.db      # app settings + configured devices/points/measurements
Data/Data_yyyyMMdd.db     # daily measurement archive
Logs/                     # optional diagnostic logs
```

## Next Docs

- [docs/CONFIGURATION.md](docs/CONFIGURATION.md)
- [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)
- [docs/FAQ.md](docs/FAQ.md)
