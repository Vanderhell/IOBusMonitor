# SQLite Storage

## Scope

This repository stores polled measurements in daily SQLite files named `Data_yyyyMMdd.db`. This task does not change that naming convention.

## Runtime initialization

`DataStorageService` now initializes storage on every save path against the target DB file, including already existing files.

- `MeasurementData` is created with `CREATE TABLE IF NOT EXISTS`
- `PRAGMA user_version = 1` is used as the schema version marker
- missing indexes are created with `CREATE INDEX IF NOT EXISTS`

## Connection PRAGMAs

The storage connection applies these PRAGMAs for local logging workloads:

- `journal_mode = WAL`
- `synchronous = NORMAL`
- `busy_timeout = 5000`

`synchronous = NORMAL` is the chosen durability/performance tradeoff documented for this task.

## Insert behavior

- Each point save uses one explicit SQLite transaction
- rows for the point are inserted through a prepared parameterized command
- timestamp and numeric parameters use explicit `DbType` assignments instead of `AddWithValue`

## Indexes

These indexes are created for common history lookups:

- `IX_MeasurementData_Timestamp` on `(Timestamp)`
- `IX_MeasurementData_PointHistory` on `(DeviceId, PointId, PointType, Timestamp)`
- `IX_MeasurementData_MeasurementHistory` on `(DeviceId, PointId, MeasurementId, PointType, Timestamp)`

These indexes align with current history queries that filter by device/point, optionally point type or measurement, and order by timestamp.
