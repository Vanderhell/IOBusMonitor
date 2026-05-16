# TASK 12 — README, docs and GitHub presentation

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

Turn the repository page into a credible acquisition page for users and service leads.

## Context from audit

Current README has screenshots and a basic overview, but it does not clearly sell the target workflow, limitations, download path, demo mode, or support model.

## Required changes

- Replace README using `templates/README_NEW.md` as structure, adjusted to actual implemented state only.
- Add `docs/GETTING_STARTED.md`, `docs/CONFIGURATION.md`, `docs/TROUBLESHOOTING.md`, `docs/FAQ.md`.
- Add clear limitations: monitoring/logging tool, not SCADA replacement, read-only unless implemented otherwise.
- Add screenshots after UI redesign; do not reuse outdated screenshots if UI changed.
- Add badges only for real CI/release/license facts.
- Add GitHub topics: modbus, modbus-tcp, modbus-rtu, siemens-s7, plc, wpf, sqlite, industrial-automation.

## Acceptance criteria

- README says exactly what the app does and who it is for.
- New user can find download/build/demo instructions quickly.
- No unsupported marketing claims are present.
- Docs match current code.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
