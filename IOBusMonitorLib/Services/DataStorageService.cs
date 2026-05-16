using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Persists measurement rows to a daily SQLite file
    /// (Data_yyyyMMdd.db) inside the folder defined in <see cref="AppSettings.PathData"/>.
    /// </summary>
    public class DataStorageService
    {
        private const int CurrentSchemaVersion = 1;
        private const int BusyTimeoutMs = 5000;

        // ---------------- path helpers --------------------------------------

        private string GetDatabasePath()
        {
            var settings = new SettingsService().LoadSettings();

            string folder = string.IsNullOrEmpty(settings.PathData)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
                : settings.PathData;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string date = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(folder, "Data_" + date + ".db");
        }

        // ---------------- schema helper -------------------------------------

        private SQLiteConnection OpenConnection(string dbFile, string operation)
        {
            var conn = new SQLiteConnection("Data Source=" + dbFile + ";");
            conn.Open();

            try
            {
                ApplyConnectionPragmas(conn);
            }
            catch (Exception ex)
            {
                LogService.LogError(
                    $"Failed to apply SQLite PRAGMAs for '{dbFile}' during {operation}: {ex.Message}");
            }

            return conn;
        }

        private static void ApplyConnectionPragmas(SQLiteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
@"PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA busy_timeout = " + BusyTimeoutMs + ";";
                cmd.ExecuteNonQuery();
            }
        }

        private void EnsureDatabaseSchema(SQLiteConnection conn, string dbFile)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
@"CREATE TABLE IF NOT EXISTS MeasurementData (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Timestamp       DATETIME,
      DeviceId        INTEGER,
      PointId         INTEGER,
      MeasurementId   INTEGER,
      DeviceName      TEXT,
      PointName       TEXT,
      MeasurementName TEXT,
      Value           REAL,
      Unit            TEXT,
      PointType       INTEGER
  );
  CREATE INDEX IF NOT EXISTS IX_MeasurementData_Timestamp
      ON MeasurementData (Timestamp);
  CREATE INDEX IF NOT EXISTS IX_MeasurementData_PointHistory
      ON MeasurementData (DeviceId, PointId, PointType, Timestamp);
  CREATE INDEX IF NOT EXISTS IX_MeasurementData_MeasurementHistory
      ON MeasurementData (DeviceId, PointId, MeasurementId, PointType, Timestamp);";
                    cmd.ExecuteNonQuery();
                }

                long userVersion;
                using (var versionCmd = conn.CreateCommand())
                {
                    versionCmd.CommandText = "PRAGMA user_version;";
                    object result = versionCmd.ExecuteScalar();
                    userVersion = Convert.ToInt64(result);
                }

                if (userVersion < CurrentSchemaVersion)
                {
                    using (var versionCmd = conn.CreateCommand())
                    {
                        versionCmd.CommandText = "PRAGMA user_version = " + CurrentSchemaVersion;
                        versionCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError(
                    $"Database schema check failed for '{dbFile}': {ex.Message}");
            }
        }

        // ---------------- insert helper -------------------------------------

        private void InsertMeasurements(string dbFile,
                                        PointViewModel point,
                                        PointType type)
        {
            if (point == null || point.Measurements == null || point.Measurements.Count == 0)
                return;

            try
            {
                using (var conn = OpenConnection(dbFile, "measurement insert"))
                {
                    EnsureDatabaseSchema(conn, dbFile);

                    using (var tx = conn.BeginTransaction())
                    using (var insert = conn.CreateCommand())
                    {
                        insert.Transaction = tx;
                        insert.CommandText =
@"INSERT INTO MeasurementData
  (Timestamp, DeviceId, PointId, MeasurementId,
   DeviceName, PointName, MeasurementName,
   Value, Unit, PointType)
  VALUES
  (@Ts, @DevId, @PtId, @MeasId,
   @DevName, @PtName, @MeasName,
   @Val, @Unit, @Type)";

                        var timestampParam = insert.Parameters.Add("@Ts", DbType.DateTime);
                        var deviceIdParam = insert.Parameters.Add("@DevId", DbType.Int32);
                        var pointIdParam = insert.Parameters.Add("@PtId", DbType.Int32);
                        var measurementIdParam = insert.Parameters.Add("@MeasId", DbType.Int32);
                        var deviceNameParam = insert.Parameters.Add("@DevName", DbType.String);
                        var pointNameParam = insert.Parameters.Add("@PtName", DbType.String);
                        var measurementNameParam = insert.Parameters.Add("@MeasName", DbType.String);
                        var valueParam = insert.Parameters.Add("@Val", DbType.Double);
                        var unitParam = insert.Parameters.Add("@Unit", DbType.String);
                        var typeParam = insert.Parameters.Add("@Type", DbType.Int32);

                        foreach (var m in point.Measurements)
                        {
                            timestampParam.Value = m.Timestamp;
                            deviceIdParam.Value = point.DeviceId;
                            pointIdParam.Value = point.PointId;
                            measurementIdParam.Value = m.Id;
                            deviceNameParam.Value = (object)point.DeviceName ?? DBNull.Value;
                            pointNameParam.Value = (object)point.PointName ?? DBNull.Value;
                            measurementNameParam.Value = (object)m.Name ?? DBNull.Value;
                            valueParam.Value = m.Value;
                            unitParam.Value = (object)m.Unit ?? DBNull.Value;
                            typeParam.Value = (int)type;

                            insert.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                string pointName = point != null ? point.PointName : "<null>";
                LogService.LogError(
                    $"Failed to store measurements in '{dbFile}' for point '{pointName}' ({type}): {ex.Message}");
            }
        }

        // ---------------- public API ----------------------------------------

        public void SaveModbusTCPData(PointViewModel point)
        {
            string db = GetDatabasePath();
            InsertMeasurements(db, point, PointType.ModbusTCP);
        }

        public void SaveModbusRTUData(PointViewModel point)   // typo fixed
        {
            string db = GetDatabasePath();
            InsertMeasurements(db, point, PointType.ModbusRTU);
        }

        public void SaveSimensData(PointViewModel point)
        {
            string db = GetDatabasePath();
            InsertMeasurements(db, point, PointType.S7);
        }
    }
}
