using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Periodically scans every configured point (TCP, RTU, S7),
    /// raises <see cref="PointRead"/> live events and persists data.
    /// </summary>
    public class TimerService
    {
        private sealed class DeviceFailureState
        {
            public int ConsecutiveFailures { get; set; }
            public DateTime CooldownUntilUtc { get; set; }
            public DateTime? LastErrorUtc { get; set; }
            public string LastErrorMessage { get; set; }
            public DateTime? LastLoggedUtc { get; set; }
        }

        private const int FailureThresholdBeforeCooldown = 3;
        private static readonly TimeSpan FailureCooldown = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan FailureLogInterval = TimeSpan.FromSeconds(30);

        private CancellationTokenSource _cts;
        private bool _isRunning;
        private readonly object _lock = new object();
        private readonly Dictionary<string, DeviceFailureState> _deviceFailures = new Dictionary<string, DeviceFailureState>();

        private List<ModbusTCPPoint> _modbusTCPPoints = new List<ModbusTCPPoint>();
        private List<ModbusRTUPoint> _modbusRTUPoints = new List<ModbusRTUPoint>();
        private List<SimensPoint> _simensPoints = new List<SimensPoint>();

        private readonly SettingsService _settingsService = new SettingsService();
        private readonly DataStorageService _storageService = new DataStorageService();
        private readonly DemoModeService _demoModeService = new DemoModeService();

        /// <summary>Current live snapshot for the GUI.</summary>
        public List<PointViewModel> LivePoints { get; } = new List<PointViewModel>();

        /// <summary>Raised after each point read.</summary>
        public event Action<PointViewModel> PointRead;

        public bool IsRunning => _isRunning;

        private AppSettings _currentSettings;

        // ---------------- loading all points ----------------

        private void LoadAllPoints()
        {
            string dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                         "Settings", "Settings.db");
            if (!File.Exists(dbFile))
            {
                _modbusTCPPoints = new List<ModbusTCPPoint>();
                _modbusRTUPoints = new List<ModbusRTUPoint>();
                _simensPoints = new List<SimensPoint>();
                return;
            }

            _modbusTCPPoints = LoadModbusTCPPoints(dbFile);
            _modbusRTUPoints = LoadModbusRTUPoints(dbFile);
            _simensPoints = LoadSimensPoints(dbFile);
        }

        #region Load point helpers (TCP / RTU / S7)
        // Each method builds device–>point–>measurement hierarchy from Settings.db
        private List<ModbusTCPPoint> LoadModbusTCPPoints(string dbFile)
        {
            var points = new List<ModbusTCPPoint>();

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
    @"SELECT p.Id   AS PointId, p.Name AS PointName, p.ModbusTCPDeviceId,
             d.Name AS DeviceName, d.Active, d.IPAddress, d.Port,
             m.Id   AS MeasurementId, m.Name AS MeasurementName, m.Unit, m.Round, m.Condition,
             m.Register, m.Quantity, m.Active, m.BitOrder
      FROM ModbusTCPPoint p
      JOIN ModbusTCPDevice d ON p.ModbusTCPDeviceId = d.Id
      LEFT JOIN TCPMeasurement m ON m.ModbusTCPPointId = p.Id
      WHERE d.Active = 1 AND m.Active = 1";

                        using (var r = cmd.ExecuteReader())
                        {
                            var dict = new Dictionary<int, ModbusTCPPoint>();

                            int ordPointId = r.GetOrdinal("PointId");
                            int ordDeviceId = r.GetOrdinal("ModbusTCPDeviceId");

                            while (r.Read())
                            {
                                int pid = r.GetInt32(ordPointId);

                                ModbusTCPPoint point;
                                if (!dict.TryGetValue(pid, out point))
                                {
                                    var dev = new ModbusTCPDevice
                                    {
                                        Id = r.GetInt32(ordDeviceId),
                                        Name = r["DeviceName"].ToString(),
                                        IPAddress = r["IPAddress"].ToString(),
                                        Port = r.GetInt32(r.GetOrdinal("Port")),
                                        Active = r.GetInt32(r.GetOrdinal("Active")) == 1
                                    };

                                    point = new ModbusTCPPoint
                                    {
                                        Id = pid,
                                        Name = r["PointName"].ToString(),
                                        ModbusTCPDevice = dev,
                                        TCPMeasurements = new List<TCPMeasurement>()
                                    };

                                    dict.Add(pid, point);
                                    points.Add(point);
                                }

                                if (r["MeasurementId"] != DBNull.Value)
                                {
                                    point.TCPMeasurements.Add(new TCPMeasurement
                                    {
                                        Id = r.GetInt32(r.GetOrdinal("MeasurementId")),
                                        Name = r["MeasurementName"].ToString(),
                                        Unit = r["Unit"].ToString(),
                                        Round = r.GetInt32(r.GetOrdinal("Round")),
                                        Condition = r["Condition"].ToString(),
                                        Register = r.GetInt32(r.GetOrdinal("Register")),
                                        Quantity = r.GetInt32(r.GetOrdinal("Quantity")),
                                        Active = Convert.ToBoolean(r["Active"]),
                                        BitOrder = (BitOrder)r.GetInt32(r.GetOrdinal("BitOrder"))
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("LoadModbusTCPPoints: " + ex.Message);
            }
            return points;
        }

        // ------------------------------------------------------------
        // Load all active Modbus-RTU points + measurements + device info
        // ------------------------------------------------------------
        private List<ModbusRTUPoint> LoadModbusRTUPoints(string dbFile)
        {
            var points = new List<ModbusRTUPoint>();

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
            @"SELECT p.Id AS PointId, p.Name AS PointName, p.ModbusRTUDeviceId,
             d.Name AS DeviceName, d.Active, d.SerialPort, d.BaudRate,
             d.Parity, d.SlaveId,
             m.Id AS MeasurementId, m.Name AS MeasurementName, m.Unit,
             m.Round, m.Condition, m.Register, m.Quantity,
             m.Active, m.BitOrder
      FROM ModbusRTUPoint p
      JOIN ModbusRTUDevice d ON p.ModbusRTUDeviceId = d.Id
      LEFT JOIN RTUMeasurement m ON m.ModbusRTUPointId = p.Id
      WHERE d.Active = 1 AND m.Active = 1";

                        using (var r = cmd.ExecuteReader())
                        {
                            var dict = new Dictionary<int, ModbusRTUPoint>();

                            int ordPointId = r.GetOrdinal("PointId");
                            int ordDeviceId = r.GetOrdinal("ModbusRTUDeviceId");

                            while (r.Read())
                            {
                                int pid = r.GetInt32(ordPointId);

                                ModbusRTUPoint pt;
                                if (!dict.TryGetValue(pid, out pt))
                                {
                                    var dev = new ModbusRTUDevice
                                    {
                                        Id = r.GetInt32(ordDeviceId),
                                        Name = r["DeviceName"].ToString(),
                                        SerialPort = (SerialPortName)r.GetInt32(r.GetOrdinal("SerialPort")),
                                        BaudRate = (BaudRate)r.GetInt32(r.GetOrdinal("BaudRate")),
                                        Parity = (SerialParity)r.GetInt32(r.GetOrdinal("Parity")),
                                        SlaveId = r.GetInt32(r.GetOrdinal("SlaveId")),
                                        Active = r.GetInt32(r.GetOrdinal("Active")) == 1
                                    };

                                    pt = new ModbusRTUPoint
                                    {
                                        Id = pid,
                                        Name = r["PointName"].ToString(),
                                        ModbusRTUDevice = dev,
                                        RTUMeasurements = new List<RTUMeasurement>()
                                    };

                                    dict.Add(pid, pt);
                                    points.Add(pt);
                                }

                                if (r["MeasurementId"] != DBNull.Value)
                                {
                                    pt.RTUMeasurements.Add(new RTUMeasurement
                                    {
                                        Id = r.GetInt32(r.GetOrdinal("MeasurementId")),
                                        Name = r["MeasurementName"].ToString(),
                                        Unit = r["Unit"].ToString(),
                                        Round = r.GetInt32(r.GetOrdinal("Round")),
                                        Condition = r["Condition"].ToString(),
                                        Register = r.GetInt32(r.GetOrdinal("Register")),
                                        Quantity = r.GetInt32(r.GetOrdinal("Quantity")),
                                        Active = Convert.ToBoolean(r["Active"]),
                                        BitOrder = (BitOrder)r.GetInt32(r.GetOrdinal("BitOrder"))
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("LoadModbusRTUPoints: " + ex.Message);
            }

            return points;
        }

        // ------------------------------------------------------------
        // Load all active Siemens-S7 points + measurements + device info
        // ------------------------------------------------------------
        private List<SimensPoint> LoadSimensPoints(string dbFile)
        {
            var points = new List<SimensPoint>();

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
            @"SELECT p.Id AS PointId, p.Name AS PointName, p.SimenseDeviceId,
             d.Name AS DeviceName, d.IPAddress, d.Active, d.Port,
             d.Rack, d.Slot, d.CpuType,
             m.Id AS MeasurementId, m.Name AS MeasurementName, m.Unit,
             m.Round, m.Condition, m.Address
      FROM SimensPoint p
      JOIN SimensDevice d ON p.SimenseDeviceId = d.Id
      LEFT JOIN SimensMeasurement m ON m.SimensPointId = p.Id
      WHERE d.Active = 1 AND m.Active = 1";

                        using (var r = cmd.ExecuteReader())
                        {
                            var dict = new Dictionary<int, SimensPoint>();

                            int ordPointId = r.GetOrdinal("PointId");
                            int ordDeviceId = r.GetOrdinal("SimenseDeviceId");

                            while (r.Read())
                            {
                                int pid = r.GetInt32(ordPointId);

                                SimensPoint pt;
                                if (!dict.TryGetValue(pid, out pt))
                                {
                                    var dev = new SimensDevice
                                    {
                                        Id = r.GetInt32(ordDeviceId),
                                        Name = r["DeviceName"].ToString(),
                                        IPAddress = r["IPAddress"].ToString(),
                                        Port = r.GetInt32(r.GetOrdinal("Port")),
                                        Rack = r.GetInt32(r.GetOrdinal("Rack")),
                                        Slot = r.GetInt32(r.GetOrdinal("Slot")),
                                        CpuType = (CpuType)r.GetInt32(r.GetOrdinal("CpuType")),
                                        Active = r.GetInt32(r.GetOrdinal("Active")) == 1
                                    };

                                    pt = new SimensPoint
                                    {
                                        Id = pid,
                                        Name = r["PointName"].ToString(),
                                        SimensDevice = dev,
                                        SimensMeasurements = new List<SimensMeasurement>()
                                    };

                                    dict.Add(pid, pt);
                                    points.Add(pt);
                                }

                                if (r["MeasurementId"] != DBNull.Value)
                                {
                                    pt.SimensMeasurements.Add(new SimensMeasurement
                                    {
                                        Id = r.GetInt32(r.GetOrdinal("MeasurementId")),
                                        Name = r["MeasurementName"].ToString(),
                                        Unit = r["Unit"].ToString(),
                                        Round = r.GetInt32(r.GetOrdinal("Round")),
                                        Condition = r["Condition"].ToString(),
                                        Address = r["Address"].ToString(),
                                        Active = true          // column Active already filtered in WHERE
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("LoadSimensPoints: " + ex.Message);
            }

            return points;
        }

        #endregion

        // ---------------- main loop ----------------
        private async Task LoopAsync(CancellationToken token)
        {
            _currentSettings = _settingsService.LoadSettings();
            int intervalMs = Math.Max(_currentSettings.ReadIntervalMs, 500);

            while (!token.IsCancellationRequested)
            {
                try { await ScanAllPointsAsync(); }
                catch (Exception ex)
                {
                    LogService.LogError("Error while reading points: " + ex.Message);
                }

                try { await Task.Delay(intervalMs, token); }
                catch (TaskCanceledException) { /* ignore */ }
            }
        }

        // ---------------- public control ----------------
        public void ReloadSettings()
        {
            lock (_lock)
            {
                try
                {
                    _currentSettings = _settingsService.LoadSettings();
                    if (_currentSettings.DemoModeEnabled)
                        _demoModeService.EnsureDemoConfiguration(resetDemoData: false);
                    LoadAllPoints();
                    LogService.LogInfo("TimerService reloaded settings and points.");
                }
                catch (Exception ex)
                {
                    LogService.LogError("Failed to reload settings: " + ex.Message);
                }
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _currentSettings = _settingsService.LoadSettings();
                if (_currentSettings.DemoModeEnabled)
                    _demoModeService.EnsureDemoConfiguration(resetDemoData: false);
                LoadAllPoints();
                _cts = new CancellationTokenSource();
                _isRunning = true;
                Task.Run(() => LoopAsync(_cts.Token));
                LogService.LogInfo("TimerService started.");
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to start TimerService: " + ex.Message);
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _cts.Cancel();
                _isRunning = false;
                LogService.LogInfo("TimerService stopped.");
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to stop TimerService: " + ex.Message);
            }
        }

        // ---------------- scan helpers ----------------
        private async Task ScanAllPointsAsync()
        {
            if (_currentSettings != null && _currentSettings.DemoModeEnabled)
            {
                await ScanDemoPointsAsync();
                return;
            }

            // TCP
            foreach (var p in _modbusTCPPoints ?? Enumerable.Empty<ModbusTCPPoint>()) await ReadPointAsync(
                () => new ModbusTCPReadService(), p,
                _storageService.SaveModbusTCPData, "Modbus TCP");

            // RTU
            foreach (var p in _modbusRTUPoints ?? Enumerable.Empty<ModbusRTUPoint>()) await ReadPointAsync(
                () => new ModbusRTUReadService(), p,
                _storageService.SaveModbusRTUData, "Modbus RTU");

            // Siemens
            foreach (var p in _simensPoints ?? Enumerable.Empty<SimensPoint>()) await ReadPointAsync(
                () => new SimensReadService(), p,
                _storageService.SaveSimensData, "Siemens");
        }

        private Task ScanDemoPointsAsync()
        {
            DateTime now = DateTime.Now;

            foreach (var point in _modbusTCPPoints ?? Enumerable.Empty<ModbusTCPPoint>())
                PublishDemoPoint(BuildDemoPoint(point, now), _storageService.SaveModbusTCPData);

            foreach (var point in _modbusRTUPoints ?? Enumerable.Empty<ModbusRTUPoint>())
                PublishDemoPoint(BuildDemoPoint(point, now), _storageService.SaveModbusRTUData);

            foreach (var point in _simensPoints ?? Enumerable.Empty<SimensPoint>())
                PublishDemoPoint(BuildDemoPoint(point, now), _storageService.SaveSimensData);

            return Task.CompletedTask;
        }

        private void PublishDemoPoint(PointViewModel pointData, Action<PointViewModel> saveAction)
        {
            saveAction(pointData);
            UpdateLivePoint(pointData);
            PointRead?.Invoke(pointData);
        }

        private PointViewModel BuildDemoPoint(ModbusTCPPoint point, DateTime now)
        {
            return BuildDemoPointCore(
                point.Id,
                point.ModbusTCPDeviceId,
                point.ModbusTCPDevice.Name,
                point.Name,
                PointType.ModbusTCP,
                now,
                point.TCPMeasurements.Select(m => new DemoMeasurementDefinition
                {
                    Id = m.Id,
                    Name = m.Name,
                    Unit = m.Unit,
                    Round = m.Round,
                    SignalKind = ResolveSignalKind(m.Name, m.Unit, false)
                }).ToList());
        }

        private PointViewModel BuildDemoPoint(ModbusRTUPoint point, DateTime now)
        {
            return BuildDemoPointCore(
                point.Id,
                point.ModbusRTUDeviceId,
                point.ModbusRTUDevice.Name,
                point.Name,
                PointType.ModbusRTU,
                now,
                point.RTUMeasurements.Select(m => new DemoMeasurementDefinition
                {
                    Id = m.Id,
                    Name = m.Name,
                    Unit = m.Unit,
                    Round = m.Round,
                    SignalKind = ResolveSignalKind(m.Name, m.Unit, false)
                }).ToList());
        }

        private PointViewModel BuildDemoPoint(SimensPoint point, DateTime now)
        {
            return BuildDemoPointCore(
                point.Id,
                point.SimenseDeviceId,
                point.SimensDevice.Name,
                point.Name,
                PointType.S7,
                now,
                point.SimensMeasurements.Select(m => new DemoMeasurementDefinition
                {
                    Id = m.Id,
                    Name = m.Name,
                    Unit = m.Unit,
                    Round = m.Round,
                    SignalKind = ResolveSignalKind(m.Name, m.Unit, SimensAddressHelper.GetDataTypeFromAddress(m.Address) == DataType.Bit)
                }).ToList());
        }

        private PointViewModel BuildDemoPointCore(int pointId, int deviceId, string deviceName, string pointName, PointType pointType, DateTime now, List<DemoMeasurementDefinition> measurements)
        {
            var point = new PointViewModel
            {
                PointId = pointId,
                DeviceId = deviceId,
                DeviceName = deviceName,
                PointName = pointName,
                Type = pointType,
                Timestamp = now,
                LastScan = now,
                LastSuccessUtc = DateTime.UtcNow,
                Status = PointStatus.Online,
                ConsecutiveFailures = 0,
                Measurements = new System.Collections.ObjectModel.ObservableCollection<MeasurementViewModel>()
            };

            foreach (var measurement in measurements)
            {
                double value = GenerateDemoValue(deviceId, pointId, measurement.Id, measurement.SignalKind, now);
                double rounded = Math.Round(value, measurement.Round);
                point.Measurements.Add(new MeasurementViewModel
                {
                    Id = measurement.Id,
                    Name = measurement.Name,
                    Unit = measurement.Unit,
                    Value = rounded,
                    ValueStr = rounded.ToString("F" + measurement.Round),
                    Timestamp = now
                });
            }

            return point;
        }

        private double GenerateDemoValue(int deviceId, int pointId, int measurementId, DemoSignalKind signalKind, DateTime now)
        {
            double seconds = (now - new DateTime(2024, 1, 1)).TotalSeconds;
            double phase = (deviceId * 0.47) + (pointId * 0.19) + (measurementId * 0.31);

            switch (signalKind)
            {
                case DemoSignalKind.Temperature:
                    return 62 + Math.Sin(seconds / 18d + phase) * 8 + Math.Cos(seconds / 50d + phase) * 1.8;
                case DemoSignalKind.Pressure:
                    return 5.5 + Math.Sin(seconds / 14d + phase) * 0.7 + Math.Cos(seconds / 41d + phase) * 0.2;
                case DemoSignalKind.Flow:
                    return 120 + Math.Sin(seconds / 11d + phase) * 18 + Math.Cos(seconds / 37d + phase) * 6;
                case DemoSignalKind.Speed:
                    return 1480 + Math.Sin(seconds / 9d + phase) * 90 + Math.Cos(seconds / 27d + phase) * 30;
                case DemoSignalKind.Current:
                    return 18 + Math.Sin(seconds / 12d + phase) * 2.2 + Math.Cos(seconds / 44d + phase) * 0.5;
                case DemoSignalKind.Percent:
                    return 74 + Math.Sin(seconds / 16d + phase) * 12 + Math.Cos(seconds / 29d + phase) * 4;
                case DemoSignalKind.Level:
                    return 58 + Math.Sin(seconds / 20d + phase) * 15 + Math.Cos(seconds / 65d + phase) * 3;
                case DemoSignalKind.Boolean:
                    return Math.Sin(seconds / 8d + phase) >= 0 ? 1d : 0d;
                default:
                    return 40 + Math.Sin(seconds / 15d + phase) * 10;
            }
        }

        private DemoSignalKind ResolveSignalKind(string name, string unit, bool isBoolean)
        {
            if (isBoolean)
                return DemoSignalKind.Boolean;

            string sample = ((name ?? string.Empty) + " " + (unit ?? string.Empty)).ToLowerInvariant();
            if (sample.Contains("temp") || sample.Contains("°c"))
                return DemoSignalKind.Temperature;
            if (sample.Contains("press") || sample.Contains("bar"))
                return DemoSignalKind.Pressure;
            if (sample.Contains("flow") || sample.Contains("l/min"))
                return DemoSignalKind.Flow;
            if (sample.Contains("speed") || sample.Contains("rpm"))
                return DemoSignalKind.Speed;
            if (sample.Contains("current") || sample.Contains(" a"))
                return DemoSignalKind.Current;
            if (sample.Contains("%") || sample.Contains("quality"))
                return DemoSignalKind.Percent;
            if (sample.Contains("level"))
                return DemoSignalKind.Level;
            return DemoSignalKind.Generic;
        }

        /// <summary>
        /// Generic utility that reads one point, stores the data and raises events.
        /// </summary>
        private async Task ReadPointAsync(
            Func<IDisposable> readerFactory,
            dynamic point,
            Action<PointViewModel> saveAction,
            string protocolName)
        {
            string deviceKey = BuildDeviceKey(protocolName, point);

            if (!CanAttemptRead(deviceKey))
            {
                var skippedPoint = CreateStatusPoint(point, protocolName, PointStatus.Offline,
                    GetCooldownMessage(deviceKey));
                UpdateLivePoint(skippedPoint);
                PointRead?.Invoke(skippedPoint);
                return;
            }

            try
            {
                UpdateLivePoint(CreateStatusPoint(point, protocolName, PointStatus.Connecting, null));

                using (var reader = readerFactory())
                {
                    var pointData = await ((dynamic)reader).LoadPointDataAsync(point);
                    pointData.LastScan = DateTime.Now;
                    pointData.LastSuccessUtc = DateTime.UtcNow;
                    pointData.LastErrorUtc = null;
                    pointData.LastErrorMessage = null;
                    pointData.Status = PointStatus.Online;
                    pointData.ConsecutiveFailures = 0;

                    saveAction(pointData);
                    ClearFailureState(deviceKey);
                    UpdateLivePoint(pointData);
                    PointRead?.Invoke(pointData);
                }
            }
            catch (Exception ex)
            {
                var state = RegisterFailure(deviceKey, ex.Message);
                var status = MapStatus(ex);
                var failedPoint = CreateStatusPoint(point, protocolName, status, ex.Message);
                failedPoint.ConsecutiveFailures = state.ConsecutiveFailures;
                failedPoint.LastErrorUtc = state.LastErrorUtc;
                failedPoint.Status = state.CooldownUntilUtc > DateTime.UtcNow ? PointStatus.Offline : status;

                if (ShouldLogFailure(protocolName, point, state))
                {
                    string deviceName;
                    int deviceId;
                    GetDeviceIdentity(point, protocolName, out deviceName, out deviceId);
                    LogService.LogError(
                        "protocol=" + protocolName +
                        " device=" + deviceName +
                        " deviceId=" + deviceId +
                        " point=" + point.Name +
                        " status=" + failedPoint.Status +
                        " failures=" + state.ConsecutiveFailures +
                        " message=" + ex.Message);
                    state.LastLoggedUtc = DateTime.UtcNow;
                }

                UpdateLivePoint(failedPoint);
                PointRead?.Invoke(failedPoint);
            }
        }

        private bool CanAttemptRead(string deviceKey)
        {
            DeviceFailureState state;
            if (!_deviceFailures.TryGetValue(deviceKey, out state))
                return true;

            return state.CooldownUntilUtc <= DateTime.UtcNow;
        }

        private DeviceFailureState RegisterFailure(string deviceKey, string errorMessage)
        {
            DeviceFailureState state;
            if (!_deviceFailures.TryGetValue(deviceKey, out state))
            {
                state = new DeviceFailureState();
                _deviceFailures[deviceKey] = state;
            }

            state.ConsecutiveFailures++;
            state.LastErrorUtc = DateTime.UtcNow;
            state.LastErrorMessage = errorMessage;

            if (state.ConsecutiveFailures >= FailureThresholdBeforeCooldown)
                state.CooldownUntilUtc = DateTime.UtcNow.Add(FailureCooldown);

            return state;
        }

        private void ClearFailureState(string deviceKey)
        {
            DeviceFailureState state;
            if (_deviceFailures.TryGetValue(deviceKey, out state))
            {
                state.ConsecutiveFailures = 0;
                state.CooldownUntilUtc = DateTime.MinValue;
                state.LastErrorUtc = null;
                state.LastErrorMessage = null;
                state.LastLoggedUtc = null;
            }
        }

        private static PointStatus MapStatus(Exception ex)
        {
            var timeoutEx = ex as TimeoutException;
            if (timeoutEx != null)
                return PointStatus.Timeout;

            var plcEx = ex as PlcConnectionException;
            if (plcEx != null)
            {
                if (plcEx.Message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PointStatus.Timeout;
                return PointStatus.Offline;
            }

            return PointStatus.ReadError;
        }

        private static string BuildDeviceKey(string protocolName, dynamic point)
        {
            string deviceName;
            int deviceId;
            GetDeviceIdentity(point, protocolName, out deviceName, out deviceId);
            return protocolName + "|" + point.GetType().Name + "|" + deviceId;
        }

        private string GetCooldownMessage(string deviceKey)
        {
            DeviceFailureState state;
            if (!_deviceFailures.TryGetValue(deviceKey, out state))
                return "Skipped due to device cooldown.";

            return "Skipped due to device cooldown until " + state.CooldownUntilUtc.ToLocalTime().ToString("HH:mm:ss") + ".";
        }

        private static bool ShouldLogFailure(string protocolName, dynamic point, DeviceFailureState state)
        {
            if (!state.LastLoggedUtc.HasValue)
                return true;

            return DateTime.UtcNow - state.LastLoggedUtc.Value >= FailureLogInterval;
        }

        private PointViewModel CreateStatusPoint(dynamic point, string protocolName, PointStatus status, string errorMessage)
        {
            string deviceName;
            int deviceId;
            GetDeviceIdentity(point, protocolName, out deviceName, out deviceId);

            string deviceKey = BuildDeviceKey(protocolName, point);
            DeviceFailureState state;
            _deviceFailures.TryGetValue(deviceKey, out state);

            return new PointViewModel
            {
                DeviceId = deviceId,
                DeviceName = deviceName,
                PointId = point.Id,
                PointName = point.Name,
                Type = MapPointType(protocolName),
                Timestamp = DateTime.Now,
                LastScan = DateTime.Now,
                LastErrorUtc = state != null ? state.LastErrorUtc : DateTime.UtcNow,
                LastSuccessUtc = null,
                LastErrorMessage = errorMessage,
                ConsecutiveFailures = state != null ? state.ConsecutiveFailures : 0,
                Status = status,
                Measurements = new System.Collections.ObjectModel.ObservableCollection<MeasurementViewModel>()
            };
        }

        private static void GetDeviceIdentity(dynamic point, string fallbackProtocolName, out string deviceName, out int deviceId)
        {
            deviceName = fallbackProtocolName;
            deviceId = 0;

            try
            {
                deviceName = point.ModbusTCPDevice.Name;
                deviceId = point.ModbusTCPDeviceId;
                return;
            }
            catch { }

            try
            {
                deviceName = point.ModbusRTUDevice.Name;
                deviceId = point.ModbusRTUDeviceId;
                return;
            }
            catch { }

            try
            {
                deviceName = point.SimensDevice.Name;
                deviceId = point.SimenseDeviceId;
            }
            catch { }
        }

        private static PointType MapPointType(string protocolName)
        {
            if (protocolName == "Modbus TCP") return PointType.ModbusTCP;
            if (protocolName == "Modbus RTU") return PointType.ModbusRTU;
            return PointType.S7;
        }

        // ---------------- live-point merge ----------------
        private void UpdateLivePoint(PointViewModel newData)
        {
            lock (_lock)
            {
                var existing = LivePoints.FirstOrDefault(p =>
                    p.PointId == newData.PointId &&
                    p.DeviceId == newData.DeviceId &&
                    p.Type == newData.Type);

                if (existing != null)
                {
                    if (newData.Measurements != null && newData.Measurements.Count > 0)
                    {
                        existing.Measurements.Clear();
                        foreach (var m in newData.Measurements)
                        {
                            existing.Measurements.Add(new MeasurementViewModel
                            {
                                Id = m.Id,
                                Name = m.Name,
                                Unit = m.Unit,
                                Value = m.Value,
                                ValueStr = m.Value.ToString("F2"),
                                Timestamp = m.Timestamp
                            });
                        }
                    }
                    existing.LastScan = newData.LastScan;
                    if (newData.LastSuccessUtc.HasValue)
                        existing.LastSuccessUtc = newData.LastSuccessUtc;
                    if (newData.LastErrorUtc.HasValue || !string.IsNullOrEmpty(newData.LastErrorMessage))
                        existing.LastErrorUtc = newData.LastErrorUtc;
                    if (newData.LastErrorMessage != null || newData.Status == PointStatus.Online)
                        existing.LastErrorMessage = newData.LastErrorMessage;
                    existing.ConsecutiveFailures = newData.ConsecutiveFailures;
                    existing.Status = newData.Status;
                }
                else
                {
                    LivePoints.Add(newData);
                }
            }
        }

        private sealed class DemoMeasurementDefinition
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Unit { get; set; }
            public int Round { get; set; }
            public DemoSignalKind SignalKind { get; set; }
        }

        private enum DemoSignalKind
        {
            Generic,
            Temperature,
            Pressure,
            Flow,
            Speed,
            Current,
            Percent,
            Level,
            Boolean
        }
    }
}
