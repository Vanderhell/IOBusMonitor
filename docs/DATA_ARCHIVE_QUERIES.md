# Data Archive Queries

## Archive layout

- measurement archives live in the configured data folder
- file naming stays `Data_yyyyMMdd.db`
- each day is stored in a separate SQLite file
- missing or corrupt daily files are logged and skipped so one bad archive does not stop the whole UI

## Dashboard loading

- dashboard startup loads the latest known value per measurement
- it does not run `SELECT *` across every archive file
- latest values are resolved per `(DeviceId, PointId, MeasurementId, PointType)`

## History loading

- history queries are bounded by a requested time range
- history queries can filter by protocol, device, point, and measurement
- row retrieval is capped with a row limit before chart rendering
- optional downsampling reduces point count for plotting while keeping chronological order

## Current limits

- default history row limit: `5000`
- default chart point limit after downsampling: `1000`
- default chart time window used by the current viewmodels: last `7` days

These defaults are implementation limits, not file format changes.
