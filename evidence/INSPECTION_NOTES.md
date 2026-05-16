# Inspection evidence notes

These are source-level observations used to create the plan.

## README

- Describes target OS Windows 10/11 x64.
- Describes UI as WPF `.NET Framework 4.8, C# 7.3`.
- Describes storage as local SQLite, one daily file.
- Describes protocols: Modbus TCP, Modbus RTU, Siemens S7.
- Describes runtime folders `Settings/Settings.db` and `Data/Data_YYYYMMDD.db`.

## Solution/project files

- `IOBusMonitor.sln` contains three projects:
  - `IOBusMonitor`
  - `IOBusMonitorLib`
  - `ShortcutTool`
- `IOBusMonitor/IOBusMonitor.csproj` targets `v4.7.2`.
- `IOBusMonitorLib/IOBusMonitorLib.csproj` targets `v4.7.2`.
- packages.config files target `net472`.

## Critical code observations

### `TimerService.cs`

- `ScanAllPointsAsync()` loops over TCP, RTU and Siemens points.
- RTU branch calls `_storageService.SaveModbusTCPData`.
- Generic `ReadPointAsync()` uses `dynamic` and calls `LoadPointDataAsync(point)`.

### `SimensReadService.cs`

- Public method is named `LoadPLCActualDataAsync(SimensPoint point)`, not `LoadPointDataAsync`.
- `ReadValue()` catches failures and returns `0d`, which can hide communication failures as real zero values.

### `ModbusRTUReadService.cs`

- `LoadPointDataAsync()` creates `PointViewModel` without initializing `Measurements`.
- Method then calls `vm.Measurements.Add(...)`.

### `DataStorageService.cs`

- Daily DB path is `Data_yyyyMMdd.db`.
- Table creation occurs only if DB file is new.
- Inserts happen row by row without explicit transaction.

### `DataLoaderService.cs`

- Loads every `Data_*.db` file.
- Uses `SELECT * FROM MeasurementData`.
- Groups latest values in memory.

### WPF UI

- `MainWindow.xaml` uses `WindowStyle="None"`, emoji menu labels, hardcoded colors and font sizes.
- `DashboardPage.xaml` repeats similar UI structure for each protocol.
- Dashboard uses `ScrollViewer` + `ItemsControl`, which is not a good structure for large measurement sets.
