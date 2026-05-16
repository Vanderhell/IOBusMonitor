# TASK 04 — Data loading and history query performance

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

Replace full-archive loading with bounded, indexed queries suitable for real daily SQLite archives.

## Context from audit

`DataLoaderService.LoadAllPointsFromAllDatabases()` reads `SELECT * FROM MeasurementData` from every `Data_*.db`, parses everything in memory, then groups. This will fail with real data volume.

## Required changes

- Add query methods with date/time range, protocol, device, point and measurement filters.
- For dashboard initial load, query latest value per measurement, not all rows.
- For history chart, query only selected date range and selected measurement(s).
- Add row limit and optional downsampling/decimation for chart rendering.
- Handle missing/corrupt daily DB files by logging and continuing.
- Add docs explaining archive layout and query limits.

## Acceptance criteria

- No dashboard code path loads all rows from all DB files.
- History chart can request a bounded time range.
- Corrupt/missing archive file does not crash whole UI.
- DataLoaderService has unit-testable query-building logic or covered integration tests with temporary SQLite files.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
