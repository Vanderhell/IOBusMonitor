# FAQ

## Is IOBusMonitor A SCADA System?

No. The repository documents it as a local monitoring, logging, and troubleshooting tool, not a SCADA replacement.

## Can It Write PLC Outputs?

Current public docs should treat the app as read-only monitoring. Output writing/control is not documented as a supported workflow in this repository state.

## Can I Try It Without Hardware?

Yes. Demo mode seeds sample configuration and generates synthetic live values locally without polling real network or serial devices.

## Where Is The Configuration Stored?

Application settings and configured devices/points/measurements are stored in:

```text
Settings/Settings.db
```

## Where Are Measurements Stored?

Daily archives are written as SQLite files:

```text
Data/Data_yyyyMMdd.db
```

The archive path can be changed from the application settings page.

## Does It Need A Server?

No external service is required for the documented local workflow. The app runs as a Windows desktop executable and stores data locally.

## Can I Use It On Linux Or In WSL?

The repository does not claim Linux or WSL runtime support for the WPF application. Build and runtime documentation are Windows-focused.

## Are There Automated Tests?

Yes, but current automated coverage is focused on core library logic such as SQLite storage behavior, Modbus conversions, condition evaluation, and Siemens address handling. The public docs do not claim WPF UI rendering tests or real hardware integration tests.

## Where Do I Report Problems?

Use GitHub Issues for reproducible bugs, build failures, documentation errors, and protocol compatibility reports.
