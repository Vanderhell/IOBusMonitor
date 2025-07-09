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
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private readonly object _lock = new object();

        private List<ModbusTCPPoint> _modbusTCPPoints;
        private List<ModbusRTUPoint> _modbusRTUPoints;
        private List<SimensPoint> _simensPoints;

        private readonly SettingsService _settingsService = new SettingsService();
        private readonly DataStorageService _storageService = new DataStorageService();

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
            if (!File.Exists(dbFile)) return;

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
            // TCP
            foreach (var p in _modbusTCPPoints) await ReadPointAsync(
                () => new ModbusTCPReadService(), p,
                _storageService.SaveModbusTCPData, "Modbus TCP");

            // RTU
            foreach (var p in _modbusRTUPoints) await ReadPointAsync(
                () => new ModbusRTUReadService(), p,
                _storageService.SaveModbusTCPData, "Modbus RTU");

            // Siemens
            foreach (var p in _simensPoints) await ReadPointAsync(
                () => new SimensReadService(), p,
                _storageService.SaveSimensData, "Siemens");
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
            try
            {
                using (var reader = readerFactory())
                {
                    var pointData = await ((dynamic)reader).LoadPointDataAsync(point);
                    pointData.LastScan = DateTime.Now;

                    saveAction(pointData);
                    UpdateLivePoint(pointData);
                    PointRead?.Invoke(pointData);
                }
            }
            catch (Exception ex)
            {
                LogService.LogError(
                    $"Error reading {protocolName} point {point.Name}: {ex.Message}");
            }
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
                    existing.LastScan = newData.LastScan;
                }
                else
                {
                    LivePoints.Add(newData);
                }
            }
        }
    }
}
