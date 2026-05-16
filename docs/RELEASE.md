# Release Packaging

`IOBusMonitor` ships as a portable Windows ZIP package. The release workflow is handled by `build/package-release.ps1`.

## Prerequisites

- Windows PowerShell 5.1 or PowerShell 7+
- `nuget.exe` available on `PATH`
- `msbuild.exe` available on `PATH` from Visual Studio 2019/2022 or Build Tools

## Standard package

```powershell
.\build\package-release.ps1
```

What the script does:

1. Restores NuGet packages for `IOBusMonitor.sln`
2. Builds `Release|x64`
3. Creates a clean staging folder in `dist/staging`
4. Copies runtime binaries, configs, DLLs, and bundled manuals
5. Excludes local `Settings`, `Data`, `Logs`, `.pdb`, `.xml`, and database files
6. Produces `dist/IOBusMonitor-<version>-win-x64.zip`

## Optional checksum

```powershell
.\build\package-release.ps1 -GenerateChecksum
```

This creates `dist/IOBusMonitor-<version>-win-x64.zip.sha256` beside the ZIP.

## Optional demo package

```powershell
.\build\package-release.ps1 -IncludeDemoData
```

This additionally copies local `Settings`, `Data`, and `Logs` folders into the staged package if they exist. Use this only when intentionally distributing a pre-seeded demo build.

## GitHub Release workflow

The automated release flow lives in [`.github/workflows/release.yml`](../.github/workflows/release.yml).

- Push a tag such as `v1.0.0`
- GitHub Actions restores packages, builds `Release|x64`, runs the test project, and then runs `build/package-release.ps1 -GenerateChecksum`
- The workflow creates a GitHub Release and uploads the ZIP plus SHA-256 file
- You can also run the workflow manually from the Actions tab with `workflow_dispatch`

## Package contents

Expected package root:

```text
IOBusMonitor-<version>-win-x64/
  IOBusMonitor.exe
  IOBusMonitor.exe.config
  IOBusMonitorLib.dll
  *.dll
  x64/SQLite.Interop.dll   # when emitted by the SQLite package
  EM-07 SET MODBUS REGISTER TABLE ENG .pdf
  README.md
  LICENSE
```

The portable package does not include runtime user data by default. `Settings/`, `Data/`, and `Logs/` are created by the application as needed on first run.
