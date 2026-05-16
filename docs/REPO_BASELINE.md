# Repository Baseline

## Snapshot date

- 2026-05-15

## Solution structure

- Solution: `IOBusMonitor.sln`
- Projects:
  - `IOBusMonitor/IOBusMonitor.csproj`
  - `IOBusMonitorLib/IOBusMonitorLib.csproj`
  - `ShortcutTool/ShortcutTool.csproj`

## Target frameworks

- `IOBusMonitor`: `.NET Framework 4.7.2`
- `IOBusMonitorLib`: `.NET Framework 4.7.2`
- `ShortcutTool`: `.NET Framework 4.8`

## Project types

- `IOBusMonitor`: WPF desktop application (`OutputType=WinExe`)
- `IOBusMonitorLib`: class library (`OutputType=Library`)
- `ShortcutTool`: console/utility executable (`OutputType=Exe`)

## Package management style

- Legacy `packages.config` style is in use.
- Package manifests found:
  - `IOBusMonitor/packages.config`
  - `IOBusMonitorLib/packages.config`
- Solution expects restored packages under the repository `packages/` folder.

## Build configurations in solution

- `Debug|Any CPU`
- `Debug|x64`
- `Release|Any CPU`
- `Release|x64`

## Runtime data locations referenced by code

- `Settings/`
- `Data/`
- `Logs/`

These paths are created or consumed relative to `AppDomain.CurrentDomain.BaseDirectory`.

## Build verification result from current environment

The required restore/build commands could not be completed in the current WSL shell because `nuget` and `msbuild` are not present on `PATH`.

- Restore command result:
  - command: `nuget restore ./IOBusMonitor.sln`
  - output: `/bin/bash: line 1: nuget: command not found`
- Debug build result:
  - command: `msbuild ./IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`
  - output: `/bin/bash: line 1: msbuild: command not found`
- Release build result:
  - command: `msbuild ./IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64`
  - output: `/bin/bash: line 1: msbuild: command not found`

## Additional repository facts

- The solution declares `IOBusMonitor` depends on both `IOBusMonitorLib` and `ShortcutTool`.
- `ShortcutTool` includes a COM reference to `IWshRuntimeLibrary`.
- Existing root `.gitignore` already ignores `bin`, `obj`, `.vs`, and `packages`, but it did not yet cover runtime data folders or common user-specific Visual Studio files.
