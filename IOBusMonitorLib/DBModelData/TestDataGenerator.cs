using System;
using System.Data.SQLite;
using System.IO;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Creates a synthetic MeasurementData DB (one file per day) filled with
    /// random values. Useful for UI testing when no real PLC is connected.
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// Generates today’s <c>Data_yyyyMMdd.db</c> inside the <c>Data</c>
        /// folder if the file does not already exist.
        /// </summary>
        public static void GenerateTestData()
        {
            try
            {
                string date = DateTime.Now.ToString("yyyyMMdd");
                string dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                             "Data", $"Data_{date}.db");

                // Create folder if missing
                string folder = Path.GetDirectoryName(dbFile);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Skip if test DB already exists
                if (File.Exists(dbFile))
                    return;

                using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
                {
                    conn.Open();

                    // --- schema --------------------------------------------------
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE MeasurementData (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Timestamp      DATETIME,
                                DeviceId       INTEGER,
                                PointId        INTEGER,
                                MeasurementId  INTEGER,
                                DeviceName     TEXT,
                                PointName      TEXT,
                                MeasurementName TEXT,
                                Value          REAL,
                                Unit           TEXT,
                                PointType      INTEGER
                            )";
                        cmd.ExecuteNonQuery();
                    }

                    // --- insert random rows -------------------------------------
                    var insert = conn.CreateCommand();
                    insert.CommandText = @"
                        INSERT INTO MeasurementData
                        (Timestamp, DeviceId, PointId, MeasurementId,
                         DeviceName, PointName, MeasurementName, Value, Unit, PointType)
                        VALUES (@Timestamp, @DeviceId, @PointId, @MeasurementId,
                                @DeviceName, @PointName, @MeasurementName, @Value, @Unit, @PointType)";

                    var rnd = new Random();

                    for (int deviceType = 0; deviceType < 3; deviceType++)
                    {
                        int deviceId = deviceType + 1;
                        string deviceName = $"Device_{deviceId}";
                        string pointName = $"Point_{deviceId}";
                        int pointId = deviceId;

                        for (int mId = 1; mId <= 3; mId++)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                insert.Parameters.Clear();
                                insert.Parameters.AddWithValue("@Timestamp",
                                    DateTime.Now.AddMinutes(-i * 5));
                                insert.Parameters.AddWithValue("@DeviceId", deviceId);
                                insert.Parameters.AddWithValue("@PointId", pointId);
                                insert.Parameters.AddWithValue("@MeasurementId", mId);
                                insert.Parameters.AddWithValue("@DeviceName", deviceName);
                                insert.Parameters.AddWithValue("@PointName", pointName);
                                insert.Parameters.AddWithValue("@MeasurementName",
                                    $"Measurement_{mId}");
                                insert.Parameters.AddWithValue("@Value",
                                    rnd.NextDouble() * 100);
                                insert.Parameters.AddWithValue("@Unit", "°C");
                                insert.Parameters.AddWithValue("@PointType", deviceType);
                                insert.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silent: test-data generation should never crash the app
            }
        }
    }
}
