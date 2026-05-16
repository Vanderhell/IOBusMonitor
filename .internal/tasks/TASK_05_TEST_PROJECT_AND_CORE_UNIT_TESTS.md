# TASK 05 — Add test project and core unit tests

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

Add a basic automated proof layer so claims in README become credible.

## Context from audit

The repository currently exposes protocol conversion, condition evaluation, address parsing, storage and data loading logic but no visible test project.

## Required changes

- Add `IOBusMonitorLib.Tests` targeting .NET Framework compatible with the solution.
- Use MSTest, NUnit, or xUnit; pick one and document why.
- Add tests for `ConditionEvaluator` valid/invalid formulas.
- Add tests for Modbus word conversion helpers; make helpers internal with InternalsVisibleTo if needed.
- Add tests for Siemens address parsing/inferred data type if helper exists.
- Add tests for SQLite table creation and insertion using a temporary folder.
- Add GitHub Actions workflow on `windows-latest` that restores, builds and runs tests.

## Acceptance criteria

- Test project builds from solution.
- At least 15 meaningful tests exist.
- CI workflow exists and can run on GitHub Actions.
- No tests require real PLC hardware.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`
- `vstest.console.exe .\IOBusMonitorLib.Tests\bin\Debug\IOBusMonitorLib.Tests.dll`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
