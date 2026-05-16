using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Seeds and maintains a synthetic demo configuration so the application can
    /// run without real PLC hardware.
    /// </summary>
    public class DemoModeService
    {
        private const string DemoNamePrefix = "DEMO ";

        private string GetSettingsDbPath()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, "Settings.db");
        }

        public bool HasAnyConfiguredDevices()
        {
            string dbFile = GetSettingsDbPath();
            if (!File.Exists(dbFile))
                return false;

            using (var connection = new SQLiteConnection("Data Source=" + dbFile + ";"))
            {
                connection.Open();
                return CountRows(connection, "ModbusTCPDevice") > 0 ||
                       CountRows(connection, "ModbusRTUDevice") > 0 ||
                       CountRows(connection, "SimensDevice") > 0;
            }
        }

        public void EnsureDemoConfiguration(bool resetDemoData)
        {
            string dbFile = GetSettingsDbPath();
            if (!File.Exists(dbFile))
                return;

            using (var connection = new SQLiteConnection("Data Source=" + dbFile + ";"))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    if (resetDemoData)
                        DeleteDemoConfiguration(connection);

                    int tcpDeviceId = EnsureModbusTcpDemo(connection);
                    int rtuDeviceId = EnsureModbusRtuDemo(connection);
                    int s7DeviceId = EnsureSimensDemo(connection);

                    int tcpPointId = EnsurePoint(connection, "ModbusTCPPoint", "ModbusTCPDeviceId", tcpDeviceId, DemoNamePrefix + "Boiler Registers");
                    int rtuPointId = EnsurePoint(connection, "ModbusRTUPoint", "ModbusRTUDeviceId", rtuDeviceId, DemoNamePrefix + "Packaging Controller");
                    int s7PointId = EnsurePoint(connection, "SimensPoint", "SimenseDeviceId", s7DeviceId, DemoNamePrefix + "Tank Farm");

                    EnsureTcpMeasurement(connection, tcpPointId, DemoNamePrefix + "Boiler Temperature", "°C", 1, "value", 0, 2, BitOrder.Normal);
                    EnsureTcpMeasurement(connection, tcpPointId, DemoNamePrefix + "Line Pressure", "bar", 2, "value", 2, 2, BitOrder.Normal);
                    EnsureTcpMeasurement(connection, tcpPointId, DemoNamePrefix + "Flow Rate", "L/min", 1, "value", 4, 2, BitOrder.Normal);

                    EnsureRtuMeasurement(connection, rtuPointId, DemoNamePrefix + "Motor Speed", "rpm", 0, "value", 0, 2, BitOrder.Normal);
                    EnsureRtuMeasurement(connection, rtuPointId, DemoNamePrefix + "Drive Current", "A", 1, "value", 2, 2, BitOrder.Normal);
                    EnsureRtuMeasurement(connection, rtuPointId, DemoNamePrefix + "Output Quality", "%", 1, "value", 4, 2, BitOrder.Normal);

                    EnsureSimensMeasurement(connection, s7PointId, DemoNamePrefix + "Tank Level", "%", 1, "value", "DB1.DBD0");
                    EnsureSimensMeasurement(connection, s7PointId, DemoNamePrefix + "Tank Temperature", "°C", 1, "value", "DB1.DBD4");
                    EnsureSimensMeasurement(connection, s7PointId, DemoNamePrefix + "Valve Open", "state", 0, "value", "DB1.DBX8.0");

                    transaction.Commit();
                }
            }
        }

        public void SetDemoDeviceActiveState(bool active)
        {
            string dbFile = GetSettingsDbPath();
            if (!File.Exists(dbFile))
                return;

            using (var connection = new SQLiteConnection("Data Source=" + dbFile + ";"))
            {
                connection.Open();
                SetActiveState(connection, "ModbusTCPDevice", active);
                SetActiveState(connection, "ModbusRTUDevice", active);
                SetActiveState(connection, "SimensDevice", active);
            }
        }

        public void ResetDemoDataArchives(string dataPath)
        {
            if (string.IsNullOrWhiteSpace(dataPath) || !Directory.Exists(dataPath))
                return;

            foreach (var file in Directory.GetFiles(dataPath, "Data_*.db"))
            {
                try
                {
                    using (var connection = new SQLiteConnection("Data Source=" + file + ";"))
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "DELETE FROM MeasurementData WHERE DeviceName LIKE @Prefix";
                            command.Parameters.AddWithValue("@Prefix", DemoNamePrefix + "%");
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch
                {
                    // Ignore malformed demo archives during reset.
                }
            }
        }

        private int EnsureModbusTcpDemo(SQLiteConnection connection)
        {
            return EnsureDevice(connection,
                "ModbusTCPDevice",
                DemoNamePrefix + "Boiler Skid TCP",
                "127.0.0.1",
                502,
                null,
                null,
                null);
        }

        private int EnsureModbusRtuDemo(SQLiteConnection connection)
        {
            return EnsureDevice(connection,
                "ModbusRTUDevice",
                DemoNamePrefix + "Packaging Line RTU",
                null,
                null,
                SerialPortName.COM1,
                BaudRate.Baud19200,
                SerialParity.None,
                1);
        }

        private int EnsureSimensDemo(SQLiteConnection connection)
        {
            return EnsureDevice(connection,
                "SimensDevice",
                DemoNamePrefix + "Tank Farm S7",
                "127.0.0.1",
                102,
                0,
                1,
                CpuType.S71200);
        }

        private int EnsureDevice(SQLiteConnection connection, string table, string name, string ipAddress, int? port, int? rack, int? slot, CpuType? cpuType)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM " + table + " WHERE Name=@Name LIMIT 1";
                command.Parameters.AddWithValue("@Name", name);
                var existing = command.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    int id = Convert.ToInt32(existing, CultureInfo.InvariantCulture);
                    if (table == "ModbusTCPDevice")
                        UpdateModbusTcpDevice(connection, id, name, ipAddress, port ?? 502);
                    else
                        UpdateSimensDevice(connection, id, name, ipAddress, port ?? 102, rack ?? 0, slot ?? 1, cpuType ?? CpuType.S71200);
                    return id;
                }
            }

            if (table == "ModbusTCPDevice")
                return InsertModbusTcpDevice(connection, name, ipAddress, port ?? 502);
            return InsertSimensDevice(connection, name, ipAddress, port ?? 102, rack ?? 0, slot ?? 1, cpuType ?? CpuType.S71200);
        }

        private int EnsureDevice(SQLiteConnection connection, string table, string name, string ipAddress, int? port, SerialPortName serialPort, BaudRate baudRate, SerialParity parity, int slaveId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM " + table + " WHERE Name=@Name LIMIT 1";
                command.Parameters.AddWithValue("@Name", name);
                var existing = command.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    int id = Convert.ToInt32(existing, CultureInfo.InvariantCulture);
                    UpdateModbusRtuDevice(connection, id, name, serialPort, baudRate, parity, slaveId);
                    return id;
                }
            }

            return InsertModbusRtuDevice(connection, name, serialPort, baudRate, parity, slaveId);
        }

        private int EnsurePoint(SQLiteConnection connection, string table, string foreignKeyName, int deviceId, string pointName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM " + table + " WHERE Name=@Name AND " + foreignKeyName + "=@DeviceId LIMIT 1";
                command.Parameters.AddWithValue("@Name", pointName);
                command.Parameters.AddWithValue("@DeviceId", deviceId);
                var existing = command.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                    return Convert.ToInt32(existing, CultureInfo.InvariantCulture);
            }

            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = "INSERT INTO " + table + " (Name, " + foreignKeyName + ") VALUES (@Name, @DeviceId)";
                insert.Parameters.AddWithValue("@Name", pointName);
                insert.Parameters.AddWithValue("@DeviceId", deviceId);
                insert.ExecuteNonQuery();
            }

            using (var identity = connection.CreateCommand())
            {
                identity.CommandText = "SELECT last_insert_rowid()";
                return Convert.ToInt32(identity.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private void EnsureTcpMeasurement(SQLiteConnection connection, int pointId, string name, string unit, int round, string condition, int register, int quantity, BitOrder bitOrder)
        {
            EnsureMeasurement(connection,
                "TCPMeasurement",
                "ModbusTCPPointId",
                pointId,
                name,
                unit,
                round,
                condition,
                "Register",
                register,
                "Quantity",
                quantity,
                bitOrder,
                null);
        }

        private void EnsureRtuMeasurement(SQLiteConnection connection, int pointId, string name, string unit, int round, string condition, int register, int quantity, BitOrder bitOrder)
        {
            EnsureMeasurement(connection,
                "RTUMeasurement",
                "ModbusRTUPointId",
                pointId,
                name,
                unit,
                round,
                condition,
                "Register",
                register,
                "Quantity",
                quantity,
                bitOrder,
                null);
        }

        private void EnsureSimensMeasurement(SQLiteConnection connection, int pointId, string name, string unit, int round, string condition, string address)
        {
            EnsureMeasurement(connection,
                "SimensMeasurement",
                "SimensPointId",
                pointId,
                name,
                unit,
                round,
                condition,
                "Address",
                address,
                null,
                null,
                null,
                true);
        }

        private void EnsureMeasurement(SQLiteConnection connection, string table, string foreignKeyName, int pointId, string name, string unit, int round, string condition, string fieldOneName, object fieldOneValue, string fieldTwoName, object fieldTwoValue, BitOrder? bitOrder, bool? active)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM " + table + " WHERE Name=@Name AND " + foreignKeyName + "=@PointId LIMIT 1";
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@PointId", pointId);
                var existing = command.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    using (var update = connection.CreateCommand())
                    {
                        var updates = new List<string>
                        {
                            "Unit=@Unit",
                            "Round=@Round",
                            "Condition=@Condition"
                        };

                        if (!string.IsNullOrWhiteSpace(fieldOneName))
                            updates.Add(fieldOneName + "=@FieldOne");
                        if (!string.IsNullOrWhiteSpace(fieldTwoName))
                            updates.Add(fieldTwoName + "=@FieldTwo");
                        if (bitOrder.HasValue)
                            updates.Add("BitOrder=@BitOrder");
                        updates.Add("Active=@Active");

                        update.CommandText = "UPDATE " + table + " SET " + string.Join(", ", updates) + " WHERE Id=@Id";
                        update.Parameters.AddWithValue("@Id", Convert.ToInt32(existing, CultureInfo.InvariantCulture));
                        update.Parameters.AddWithValue("@Unit", unit);
                        update.Parameters.AddWithValue("@Round", round);
                        update.Parameters.AddWithValue("@Condition", condition);
                        if (!string.IsNullOrWhiteSpace(fieldOneName))
                            update.Parameters.AddWithValue("@FieldOne", fieldOneValue);
                        if (!string.IsNullOrWhiteSpace(fieldTwoName))
                            update.Parameters.AddWithValue("@FieldTwo", fieldTwoValue);
                        if (bitOrder.HasValue)
                            update.Parameters.AddWithValue("@BitOrder", (int)bitOrder.Value);
                        update.Parameters.AddWithValue("@Active", active.GetValueOrDefault(true) ? 1 : 0);
                        update.ExecuteNonQuery();
                    }
                    return;
                }
            }

            using (var insert = connection.CreateCommand())
            {
                var columns = new List<string> { "Name", "Unit", "Round", "Condition", foreignKeyName, "Active" };
                var values = new List<string> { "@Name", "@Unit", "@Round", "@Condition", "@PointId", "@Active" };

                if (!string.IsNullOrWhiteSpace(fieldOneName))
                {
                    columns.Add(fieldOneName);
                    values.Add("@FieldOne");
                }

                if (!string.IsNullOrWhiteSpace(fieldTwoName))
                {
                    columns.Add(fieldTwoName);
                    values.Add("@FieldTwo");
                }

                if (bitOrder.HasValue)
                {
                    columns.Add("BitOrder");
                    values.Add("@BitOrder");
                }

                insert.CommandText = "INSERT INTO " + table + " (" + string.Join(", ", columns) + ") VALUES (" + string.Join(", ", values) + ")";
                insert.Parameters.AddWithValue("@Name", name);
                insert.Parameters.AddWithValue("@Unit", unit);
                insert.Parameters.AddWithValue("@Round", round);
                insert.Parameters.AddWithValue("@Condition", condition);
                insert.Parameters.AddWithValue("@PointId", pointId);
                insert.Parameters.AddWithValue("@Active", active.GetValueOrDefault(true) ? 1 : 0);
                if (!string.IsNullOrWhiteSpace(fieldOneName))
                    insert.Parameters.AddWithValue("@FieldOne", fieldOneValue);
                if (!string.IsNullOrWhiteSpace(fieldTwoName))
                    insert.Parameters.AddWithValue("@FieldTwo", fieldTwoValue);
                if (bitOrder.HasValue)
                    insert.Parameters.AddWithValue("@BitOrder", (int)bitOrder.Value);
                insert.ExecuteNonQuery();
            }
        }

        private int InsertModbusTcpDevice(SQLiteConnection connection, string name, string ipAddress, int port)
        {
            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = "INSERT INTO ModbusTCPDevice (Name, IPAddress, Port, Active) VALUES (@Name, @IPAddress, @Port, 1)";
                insert.Parameters.AddWithValue("@Name", name);
                insert.Parameters.AddWithValue("@IPAddress", ipAddress);
                insert.Parameters.AddWithValue("@Port", port);
                insert.ExecuteNonQuery();
            }

            return GetLastInsertRowId(connection);
        }

        private void UpdateModbusTcpDevice(SQLiteConnection connection, int id, string name, string ipAddress, int port)
        {
            using (var update = connection.CreateCommand())
            {
                update.CommandText = "UPDATE ModbusTCPDevice SET Name=@Name, IPAddress=@IPAddress, Port=@Port, Active=1 WHERE Id=@Id";
                update.Parameters.AddWithValue("@Id", id);
                update.Parameters.AddWithValue("@Name", name);
                update.Parameters.AddWithValue("@IPAddress", ipAddress);
                update.Parameters.AddWithValue("@Port", port);
                update.ExecuteNonQuery();
            }
        }

        private int InsertModbusRtuDevice(SQLiteConnection connection, string name, SerialPortName serialPort, BaudRate baudRate, SerialParity parity, int slaveId)
        {
            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = "INSERT INTO ModbusRTUDevice (Name, SerialPort, BaudRate, Parity, SlaveId, Active) VALUES (@Name, @SerialPort, @BaudRate, @Parity, @SlaveId, 1)";
                insert.Parameters.AddWithValue("@Name", name);
                insert.Parameters.AddWithValue("@SerialPort", (int)serialPort);
                insert.Parameters.AddWithValue("@BaudRate", (int)baudRate);
                insert.Parameters.AddWithValue("@Parity", (int)parity);
                insert.Parameters.AddWithValue("@SlaveId", slaveId);
                insert.ExecuteNonQuery();
            }

            return GetLastInsertRowId(connection);
        }

        private void UpdateModbusRtuDevice(SQLiteConnection connection, int id, string name, SerialPortName serialPort, BaudRate baudRate, SerialParity parity, int slaveId)
        {
            using (var update = connection.CreateCommand())
            {
                update.CommandText = "UPDATE ModbusRTUDevice SET Name=@Name, SerialPort=@SerialPort, BaudRate=@BaudRate, Parity=@Parity, SlaveId=@SlaveId, Active=1 WHERE Id=@Id";
                update.Parameters.AddWithValue("@Id", id);
                update.Parameters.AddWithValue("@Name", name);
                update.Parameters.AddWithValue("@SerialPort", (int)serialPort);
                update.Parameters.AddWithValue("@BaudRate", (int)baudRate);
                update.Parameters.AddWithValue("@Parity", (int)parity);
                update.Parameters.AddWithValue("@SlaveId", slaveId);
                update.ExecuteNonQuery();
            }
        }

        private int InsertSimensDevice(SQLiteConnection connection, string name, string ipAddress, int port, int rack, int slot, CpuType cpuType)
        {
            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = "INSERT INTO SimensDevice (Name, IPAddress, Port, Rack, Slot, CpuType, Active) VALUES (@Name, @IPAddress, @Port, @Rack, @Slot, @CpuType, 1)";
                insert.Parameters.AddWithValue("@Name", name);
                insert.Parameters.AddWithValue("@IPAddress", ipAddress);
                insert.Parameters.AddWithValue("@Port", port);
                insert.Parameters.AddWithValue("@Rack", rack);
                insert.Parameters.AddWithValue("@Slot", slot);
                insert.Parameters.AddWithValue("@CpuType", (int)cpuType);
                insert.ExecuteNonQuery();
            }

            return GetLastInsertRowId(connection);
        }

        private void UpdateSimensDevice(SQLiteConnection connection, int id, string name, string ipAddress, int port, int rack, int slot, CpuType cpuType)
        {
            using (var update = connection.CreateCommand())
            {
                update.CommandText = "UPDATE SimensDevice SET Name=@Name, IPAddress=@IPAddress, Port=@Port, Rack=@Rack, Slot=@Slot, CpuType=@CpuType, Active=1 WHERE Id=@Id";
                update.Parameters.AddWithValue("@Id", id);
                update.Parameters.AddWithValue("@Name", name);
                update.Parameters.AddWithValue("@IPAddress", ipAddress);
                update.Parameters.AddWithValue("@Port", port);
                update.Parameters.AddWithValue("@Rack", rack);
                update.Parameters.AddWithValue("@Slot", slot);
                update.Parameters.AddWithValue("@CpuType", (int)cpuType);
                update.ExecuteNonQuery();
            }
        }

        private void DeleteDemoConfiguration(SQLiteConnection connection)
        {
            ExecuteNonQuery(connection, "DELETE FROM TCPMeasurement WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
            ExecuteNonQuery(connection, "DELETE FROM RTUMeasurement WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
            ExecuteNonQuery(connection, "DELETE FROM SimensMeasurement WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
            ExecuteNonQuery(connection, "DELETE FROM ModbusTCPPoint WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
            ExecuteNonQuery(connection, "DELETE FROM ModbusRTUPoint WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
            ExecuteNonQuery(connection, "DELETE FROM SimensPoint WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
            ExecuteNonQuery(connection, "DELETE FROM ModbusTCPDevice WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
            ExecuteNonQuery(connection, "DELETE FROM ModbusRTUDevice WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
            ExecuteNonQuery(connection, "DELETE FROM SimensDevice WHERE Name LIKE @Prefix", DemoNamePrefix + "%");
        }

        private void SetActiveState(SQLiteConnection connection, string table, bool active)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE " + table + " SET Active=@Active WHERE Name LIKE @Prefix";
                command.Parameters.AddWithValue("@Active", active ? 1 : 0);
                command.Parameters.AddWithValue("@Prefix", DemoNamePrefix + "%");
                command.ExecuteNonQuery();
            }
        }

        private void ExecuteNonQuery(SQLiteConnection connection, string sql, string prefix)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@Prefix", prefix);
                command.ExecuteNonQuery();
            }
        }

        private int CountRows(SQLiteConnection connection, string table)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM " + table;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private int GetLastInsertRowId(SQLiteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT last_insert_rowid()";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }
    }
}
