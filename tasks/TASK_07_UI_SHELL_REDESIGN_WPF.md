# TASK 07 — WPF shell redesign

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

Replace the hobby-style main shell with a clean technician-grade desktop layout while preserving WPF.

## Context from audit

Current MainWindow is borderless, uses emoji menu headers, hardcoded large fonts/colors, and a top menu that does not scale well.

## Required changes

- Create shared ResourceDictionary for colors, typography, spacing, buttons and cards.
- Replace emoji-heavy top menu with left navigation or clean top navigation using text + simple icons if already available.
- Add standard window controls or clearly implemented custom controls with accessible hit targets.
- Add bottom status bar: monitoring state, active devices, last scan, data path, app version.
- Preserve tray behavior but make close/minimize semantics clear.
- Make window resizable and sane on 1366x768 and 1920x1080.
- No functional regression in existing navigation commands.

## Acceptance criteria

- App opens to a professional shell without emoji menu clutter.
- All existing pages are reachable.
- Start/Stop state is obvious.
- UI resources are centralized instead of scattered hardcoded colors.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
