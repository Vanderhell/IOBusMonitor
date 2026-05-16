# Build Guide

## Scope

This repository contains a WPF desktop application and .NET Framework projects. Build and restore must be run from Windows with NuGet and MSBuild available. This document does not claim Linux or WSL build support for the WPF application.

## Verified environment facts

- Repository root solution: `IOBusMonitor.sln`
- Solution format: Visual Studio 2019 (`Format Version 12.00`, `VisualStudioVersion = 16.0.36107.64`)
- WPF app project: `IOBusMonitor/IOBusMonitor.csproj`
- Class library project: `IOBusMonitorLib/IOBusMonitorLib.csproj`
- Utility project: `ShortcutTool/ShortcutTool.csproj`

## Windows prerequisites

- Windows with Visual Studio Build Tools or Visual Studio that provides `msbuild`
- NuGet CLI available as `nuget`
- .NET Framework reference assemblies required by the projects
- COM support for `IWshRuntimeLibrary` used by `ShortcutTool`

## Restore and build commands

Run these commands from a Windows Developer Command Prompt, PowerShell, or `cmd.exe` session where `nuget` and `msbuild` are on `PATH`:

```powershell
nuget restore .\IOBusMonitor.sln
msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"
msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64
```

## Current WSL result

The required commands were attempted from the current WSL shell and did not complete because the Windows build tools are not available in this environment.

- `nuget restore ./IOBusMonitor.sln`
  - `/bin/bash: line 1: nuget: command not found`
- `msbuild ./IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`
  - `/bin/bash: line 1: msbuild: command not found`
- `msbuild ./IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64`
  - `/bin/bash: line 1: msbuild: command not found`

An additional attempt to resolve Windows tools through `cmd.exe` from this session failed with:

```text
<3>WSL (2 - ) ERROR: UtilBindVsockAnyPort:307: socket failed 1
```
