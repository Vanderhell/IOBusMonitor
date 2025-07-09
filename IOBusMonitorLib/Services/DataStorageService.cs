using System;
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

        private void EnsureDatabaseAndTableExist(string dbFile)
        {
            bool newDb = !File.Exists(dbFile);

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
                {
                    conn.Open();

                    if (newDb)
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText =
    @"CREATE TABLE MeasurementData (
          Id INTEGER PRIMARY KEY AUTOINCREMENT,
          Timestamp     DATETIME,
          DeviceId      INTEGER,
          PointId       INTEGER,
          MeasurementId INTEGER,
          DeviceName    TEXT,
          PointName     TEXT,
          MeasurementName TEXT,
          Value         REAL,
          Unit          TEXT,
          PointType     INTEGER
      )";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Database check failed: " + ex.Message);
            }
        }

        // ---------------- insert helper -------------------------------------

        private void InsertMeasurements(string dbFile,
                                        PointViewModel point,
                                        PointType type)
        {
            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
                {
                    conn.Open();
                    var insert = conn.CreateCommand();
                    insert.CommandText =
    @"INSERT INTO MeasurementData
      (Timestamp, DeviceId, PointId, MeasurementId,
       DeviceName, PointName, MeasurementName,
       Value, Unit, PointType)
      VALUES
      (@Ts, @DevId, @PtId, @MeasId,
       @DevName, @PtName, @MeasName,
       @Val, @Unit, @Type)";

                    foreach (var m in point.Measurements)
                    {
                        insert.Parameters.Clear();
                        insert.Parameters.AddWithValue("@Ts", m.Timestamp);
                        insert.Parameters.AddWithValue("@DevId", point.DeviceId);
                        insert.Parameters.AddWithValue("@PtId", point.PointId);
                        insert.Parameters.AddWithValue("@MeasId", m.Id);
                        insert.Parameters.AddWithValue("@DevName", point.DeviceName);
                        insert.Parameters.AddWithValue("@PtName", point.PointName);
                        insert.Parameters.AddWithValue("@MeasName", m.Name);
                        insert.Parameters.AddWithValue("@Val", m.Value);
                        insert.Parameters.AddWithValue("@Unit", m.Unit);
                        insert.Parameters.AddWithValue("@Type", (int)type);

                        insert.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to store measurements: " + ex.Message);
            }
        }

        // ---------------- public API ----------------------------------------

        public void SaveModbusTCPData(PointViewModel point)
        {
            string db = GetDatabasePath();
            EnsureDatabaseAndTableExist(db);
            InsertMeasurements(db, point, PointType.ModbusTCP);
        }

        public void SaveModbusRTUData(PointViewModel point)   // typo fixed
        {
            string db = GetDatabasePath();
            EnsureDatabaseAndTableExist(db);
            InsertMeasurements(db, point, PointType.ModbusRTU);
        }

        public void SaveSimensData(PointViewModel point)
        {
            string db = GetDatabasePath();
            EnsureDatabaseAndTableExist(db);
            InsertMeasurements(db, point, PointType.S7);
        }
    }
}
