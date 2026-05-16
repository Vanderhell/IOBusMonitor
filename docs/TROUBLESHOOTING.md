# Troubleshooting

## App Does Not Start

Check:

- the app was built on Windows with the required `.NET Framework` desktop tooling,
- the release ZIP or build output was copied completely,
- `IOBusMonitor.exe` is next to its required DLLs,
- `init_error.log` exists beside the executable,
- `Logs/` contains a newer diagnostic file if startup reached the logging path.

## `package-release.ps1` Fails Immediately

Check:

- `nuget.exe` is on `PATH`,
- `msbuild.exe` is on `PATH`,
- the command is being run from Windows PowerShell or PowerShell 7 on Windows,
- the shell is a Visual Studio Developer PowerShell or another environment that exposes build tools.

Current known repository fact:

- WSL in this workspace did not provide working `msbuild` execution for the WPF solution.

## No Live Values Appear

Check:

- monitoring was started,
- the selected device is active,
- the selected point is active,
- the selected measurement is active,
- the protocol settings are correct,
- the device address/register settings are valid,
- test connection or validation passes where the admin page exposes it,
- demo mode is not enabled when you expect real hardware polling.

## Demo Mode Looks Wrong

Check:

- the shell status shows `DEMO MODE`,
- `Settings` has `Demo mode` enabled,
- `Reset Demo Sample Data` was used if older demo rows should be cleared,
- your configured archive path is the one you are inspecting.

## History Page Is Slow

Current architecture notes:

- history loading is bounded by time range,
- row retrieval is capped before chart rendering,
- chart data can be downsampled,
- one corrupt daily archive should be skipped instead of breaking the whole load.

If performance still degrades, check:

- selected date range is larger than necessary,
- archive folder contains many large daily files,
- the configured data path points to slower storage.

## Values Are Missing Or Errors Repeat

Check:

- network reachability to the PLC or TCP endpoint,
- COM port availability for Modbus RTU,
- correct slave ID, baud rate, and parity,
- correct Siemens rack, slot, and CPU type,
- register/address correctness,
- Windows firewall or serial adapter permissions.

## Settings Seem To Reset

Check:

- the app can write to `Settings/Settings.db`,
- the executable folder is not locked by permissions,
- the release package was not placed in a read-only location,
- you are editing the same runtime folder that you later launch.
