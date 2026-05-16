# TASK 00 — Baseline build and repo hygiene

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

Create a verified baseline so every later change is measurable and reversible.

## Context from audit

The repo has a Visual Studio solution with WPF app, class library, and ShortcutTool. Before functional work, Codex must confirm how it builds and document exact commands.

## Required changes

- Run NuGet restore and Debug/Release builds without changing production code first.
- Create `docs/BUILD.md` with exact Windows build prerequisites and commands.
- Create `.gitignore` entries for build outputs, local DBs, Logs, Settings, Data and user-specific Visual Studio files if missing.
- Add `docs/REPO_BASELINE.md` recording current target frameworks, projects, package style, and build result.
- Do not claim Linux/WSL build support for WPF/.NET Framework.

## Acceptance criteria

- Fresh clone build steps are documented.
- Local runtime data folders are not tracked by Git.
- No production logic is changed in this task.
- Any build failure is documented with exact error text.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
