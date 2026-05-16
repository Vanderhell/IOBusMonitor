# START HERE — IOBusMonitor reform package for Codex

This ZIP is a practical execution package for repairing and positioning `IOBusMonitor` as a useful industrial desktop tool.

## Intended commercial direction

Recommended direction: **free open-source core + paid services/custom integration**.

Reason: the repository is MIT licensed and the target users are system integrators / service technicians. They are more likely to pay for:

- custom protocol/device mapping,
- on-site or remote support,
- branded builds,
- installation/package support,
- bug-fix priority,
- training and documentation,
- data export/report customization.

They are less likely to pay for a generic small open-source WPF tool before it has trust, releases, documentation, and visible proof.

## What must happen first

Do not start with UI redesign. First fix runtime correctness and release credibility:

1. Establish reproducible build.
2. Fix hard polling/storage bugs.
3. Add smoke tests and basic unit tests.
4. Stabilize SQLite storage and data loading.
5. Redesign dashboard and navigation.
6. Add sample/demo mode.
7. Package portable release.
8. Rewrite README with clear scope and screenshots.

## Recommended task order

1. `tasks/TASK_00_BASELINE_BUILD_AND_REPO_HYGIENE.md`
2. `tasks/TASK_01_FIX_TARGET_FRAMEWORK_AND_DEPENDENCY_CLEANUP.md`
3. `tasks/TASK_02_FIX_POLLING_RUNTIME_BUGS.md`
4. `tasks/TASK_03_STORAGE_SQLITE_RELIABILITY_AND_PERFORMANCE.md`
5. `tasks/TASK_04_DATA_LOADING_AND_HISTORY_PERFORMANCE.md`
6. `tasks/TASK_05_TEST_PROJECT_AND_CORE_UNIT_TESTS.md`
7. `tasks/TASK_06_DEVICE_STATUS_AND_ERROR_MODEL.md`
8. `tasks/TASK_07_UI_SHELL_REDESIGN_WPF.md`
9. `tasks/TASK_08_DASHBOARD_REDESIGN_AND_VIRTUALIZATION.md`
10. `tasks/TASK_09_ADMIN_WORKFLOW_VALIDATION_AND_TEST_CONNECTION.md`
11. `tasks/TASK_10_DEMO_MODE_AND_SAMPLE_CONFIGURATION.md`
12. `tasks/TASK_11_RELEASE_PACKAGING_AND_VERSIONING.md`
13. `tasks/TASK_12_README_DOCS_AND_GITHUB_PRESENTATION.md`
14. `tasks/TASK_13_COMMERCIAL_SUPPORT_POSITIONING.md`

## Main hard findings from repository inspection

- README says WPF `.NET Framework 4.8, C# 7.3`, but the app and library project files target `v4.7.2`.
- `TimerService.ScanAllPointsAsync()` stores Modbus RTU data through `SaveModbusTCPData` instead of `SaveModbusRTUData`.
- `TimerService.ReadPointAsync()` dynamically calls `LoadPointDataAsync`, but `SimensReadService` exposes `LoadPLCActualDataAsync`, so Siemens polling can fail at runtime.
- `ModbusRTUReadService.LoadPointDataAsync()` creates a `PointViewModel` without initializing `Measurements`, then adds to `vm.Measurements`.
- `DataLoaderService.LoadAllPointsFromAllDatabases()` loads `SELECT * FROM MeasurementData` from every daily DB, then groups in memory. This will collapse under real archives.
- `DataStorageService` opens a SQLite connection for every save and inserts rows without an explicit transaction.
- Dashboard XAML duplicates almost identical protocol UI blocks and uses `ItemsControl` inside `ScrollViewer`, which disables useful virtualization for large point counts.
- Main window uses a borderless emoji menu shell with hardcoded colors/sizes, which looks unprofessional for industrial users.

## How to use with Codex

Run one task at a time. Paste the full task file to Codex. Do not ask Codex to do the whole reform in one run.

Recommended local commands on Windows:

```powershell
nuget restore .\IOBusMonitor.sln
msbuild .\IOBusMonitor.sln /p:Configuration=Debug /p:Platform="Any CPU"
msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64
```

If tests are added:

```powershell
vstest.console.exe .\IOBusMonitorLib.Testsin\Debug\IOBusMonitorLib.Tests.dll
```
