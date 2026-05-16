# TASK 06 — Device status and error model

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

Make communication failures visible and diagnosable instead of hidden in logs.

## Context from audit

Current read services often catch exceptions and return 0/null or log only. Service technicians need status: connected, timeout, read error, last success, last error.

## Required changes

- Add status model: Unknown, Connecting, Online, Timeout, ReadError, Disabled, Offline.
- Extend live point/device view model with LastSuccessUtc, LastErrorUtc, LastErrorMessage, ConsecutiveFailures.
- Do not treat communication error as numeric value 0.
- Add per-device backoff/circuit breaker: after N failures skip for configured cooldown.
- Surface status in dashboard UI in a minimal way even before full redesign.
- Keep log entries structured enough to search by protocol/device/point.

## Acceptance criteria

- Failed communication is distinguishable from real measured zero.
- Dashboard can show which device/point is failing.
- Repeated failures do not flood log excessively.
- Start/stop remains responsive during device failures.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
