# Troubleshooting draft

## App does not start

Check:

- .NET Framework installed.
- Release ZIP was not partially copied.
- Required DLLs are next to `IOBusMonitor.exe`.
- Logs folder contains startup error.

## No live values

Check:

- Monitoring is started.
- Device is active.
- Point is active.
- Measurement is active.
- Connection test passes.
- Correct protocol selected.
- Firewall/serial port permissions.

## Values are zero

Zero must not automatically be treated as a communication error. After the error model reform, check status column:

- Online + value 0 = real measured zero.
- Timeout/ReadError + value missing = communication problem.

## History chart is slow

Expected fixes:

- bounded date range queries,
- indexes,
- downsampling,
- no dashboard startup `SELECT *` across all archives.
