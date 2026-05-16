# TASK 08 — Dashboard redesign and virtualization

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

Make the live dashboard useful for tens, hundreds, and eventually thousands of measurements.

## Context from audit

Current dashboard duplicates three almost identical protocol blocks and uses `ItemsControl` inside `ScrollViewer`, which is not good for large datasets.

## Required changes

- Create a unified dashboard item template for all protocols.
- Use a virtualized control (`DataGrid`/`ListView` with VirtualizingStackPanel) for measurement rows.
- Add search box filtering by device, point, measurement, protocol and status.
- Add protocol/status quick filters.
- Add columns: Protocol, Device, Point, Measurement, Value, Unit, Last Scan, Status.
- Add clear empty state: no configured points / monitoring stopped / no data yet.
- Keep chart buttons but reduce visual noise.

## Acceptance criteria

- Dashboard XAML no longer contains three copied protocol layouts.
- Filtering works without restarting monitoring.
- Large datasets do not create one heavy visual tree inside a ScrollViewer.
- Status and last scan are visible.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
