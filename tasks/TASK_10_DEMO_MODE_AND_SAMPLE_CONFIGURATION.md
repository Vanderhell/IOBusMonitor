# TASK 10 — Demo mode and sample configuration

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

Make the project understandable without real PLC hardware.

## Context from audit

README currently mentions calling `TestDataGenerator.GenerateTestData()` manually. That is not good enough for adoption.

## Required changes

- Add visible Demo Mode in UI or startup prompt when no devices exist.
- Create sample devices/points/measurements in Settings.db or in a separate sample config import file.
- Add synthetic live data provider that produces realistic changing values for dashboard and history.
- Ensure demo mode cannot be confused with real PLC polling; show a clear DEMO badge.
- Add one-click reset sample data option.
- Document demo mode in README.

## Acceptance criteria

- Fresh user can run app without PLC and see live changing values.
- Charts work from demo data.
- Demo mode is visually marked.
- Demo data does not poll external network/serial devices.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
