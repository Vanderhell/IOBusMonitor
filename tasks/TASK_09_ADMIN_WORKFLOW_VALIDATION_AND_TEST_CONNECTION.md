# TASK 09 — Admin workflow validation and test connection

You are Codex working inside the `Vanderhell/IOBusMonitor` repository.

EXECUTION MODE:
- Execute the task exactly as written.
- Do not redesign unrelated parts.
- Do not migrate framework/UI technology unless explicitly requested.
- Do not add unrelated features.
- Do not modify licensing.
- Do not add yourself as Author or Co-authored-by in any commit or file.
- Keep code compatible with C# 7.3 unless the project is explicitly changed.
- End with the mandatory report sections from `PROJECT_RULES.md`.

## Objective

Reduce user errors when configuring devices, points and measurements.

## Context from audit

The app targets technicians. Wrong IP/port/register/address/formula should be caught early, not after polling silently fails.

## Required changes

- Add validation for IP address/host, port range, serial settings, slave id, register, quantity, Siemens rack/slot/address.
- Add duplicate prevention where appropriate: device name + endpoint, point name per device, measurement name/register/address per point.
- Add Test Connection button for Modbus TCP and Siemens S7 device pages.
- Add Test Serial Port / port availability check for RTU where feasible without requiring hardware response.
- Add Test Formula button for condition/expression fields using sample value.
- Show validation errors inline, not only message boxes.

## Acceptance criteria

- Invalid configuration cannot be saved silently.
- Technician can test connection before starting global monitoring.
- Duplicate configuration is blocked or clearly warned.
- No database corruption from bad input.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
