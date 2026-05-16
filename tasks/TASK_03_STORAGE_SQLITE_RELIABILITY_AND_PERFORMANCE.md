# TASK 03 — SQLite storage reliability and performance

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

Make daily SQLite storage robust enough for real polling frequency and long-running use.

## Context from audit

Current `DataStorageService` opens SQLite per save, creates the table only for new DB files, inserts rows one by one without explicit transaction, and has no visible indexes.

## Required changes

- Ensure `MeasurementData` table is created if missing even when DB file already exists.
- Add schema version table or `PRAGMA user_version` for future migrations.
- Use explicit transaction per point save or per scan batch.
- Create indexes for common history queries: Timestamp, DeviceId/PointId/MeasurementId/PointType/Timestamp.
- Use parameterized commands without `AddWithValue` where practical; use explicit DbType for Timestamp and numeric values.
- Add SQLite PRAGMAs appropriate for local logging: `journal_mode=WAL`, `busy_timeout`, and document chosen `synchronous` mode.
- Do not change file naming convention `Data_yyyyMMdd.db` in this task.

## Acceptance criteria

- Storage initializes existing partial DBs safely.
- Bulk insert path is measurably less wasteful than one implicit transaction per row.
- Indexes exist and are documented.
- Storage failures are logged with DB path and operation context.

## Required verification commands

- `nuget restore .\IOBusMonitor.sln`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
