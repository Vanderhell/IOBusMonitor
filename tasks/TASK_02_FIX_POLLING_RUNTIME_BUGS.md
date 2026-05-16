# TASK 02 — Fix critical polling runtime bugs

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

Fix the concrete runtime bugs in Modbus RTU and Siemens S7 polling before any UI work.

## Context from audit

Observed issues: RTU path calls `SaveModbusTCPData`; S7 reader method name does not match dynamic call; RTU PointViewModel does not initialize `Measurements` before adding values.

## Required changes

- In `TimerService.ScanAllPointsAsync`, change RTU storage to `_storageService.SaveModbusRTUData`.
- Eliminate the S7 dynamic method mismatch by either adding a correctly named `LoadPointDataAsync(SimensPoint point)` wrapper to `SimensReadService` or by refactoring TimerService to call typed methods.
- Initialize `Measurements = new ObservableCollection<MeasurementViewModel>()` in `ModbusRTUReadService.LoadPointDataAsync`.
- Add defensive null handling for point lists in `ScanAllPointsAsync` so missing Settings.db does not cause null enumeration.
- Preserve existing public model classes.

## Acceptance criteria

- RTU saves as `PointType.ModbusRTU`.
- S7 polling path compiles and the called method exists at compile time or is covered by a test/smoke call.
- RTU LoadPointDataAsync no longer throws due to null Measurements.
- Start/Stop can run without unhandled exceptions when no devices are configured.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
