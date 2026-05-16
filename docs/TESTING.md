# Testing

## Framework choice

This repository uses `MSTest` for `IOBusMonitorLib.Tests`.

Reason:

- it is native to the Visual Studio and `vstest.console.exe` workflow already expected by this repository task set
- it works with `.NET Framework 4.7.2`
- it keeps the Windows CI path straightforward for restore, build, and test execution on GitHub Actions

## Test scope

Current automated coverage focuses on core logic that does not require real PLC hardware:

- condition expression evaluation
- Modbus register word conversion helpers
- Siemens address type inference
- serial parity/port mapping helpers
- SQLite archive creation, schema initialization, indexed inserts, latest-value loading, and bounded history loading

## Current limitations

- tests do not exercise WPF UI rendering
- tests do not require or use physical Modbus or Siemens devices
- local execution still requires Windows build tools for the full restore/build/test command chain used by the solution
