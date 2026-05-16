# TASK 01 — Fix target framework mismatch and dependency cleanup

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

Make project metadata honest and reduce dependency/package confusion without changing app behavior.

## Context from audit

README states WPF .NET Framework 4.8, but both app and library project files target v4.7.2. packages.config also targets net472. This mismatch damages trust.

## Required changes

- Choose one target: either update README to `.NET Framework 4.7.2` or retarget all projects/packages to `.NET Framework 4.8`.
- Prefer the lower-risk route unless local build proves 4.8 works: keep net472 and correct README.
- Remove unused duplicate UI package references only if build proves they are unused.
- Do not remove protocol/runtime dependencies unless compile and smoke checks pass.
- Add dependency table to `docs/DEPENDENCIES.md`: package, purpose, project, keep/remove decision.

## Acceptance criteria

- Project files and README no longer contradict each other.
- Dependency document exists and is fact-based.
- Build still passes after any package/reference changes.
- No user-facing behavior changed.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
