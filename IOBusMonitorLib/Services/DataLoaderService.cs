using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Reads the daily data files (Data_yyyyMMdd.db) and
    /// returns the latest value for every measurement of every point.
    /// </summary>
    public class DataLoaderService
    {
        /// <summary>
        /// Loads all historical databases found in <c>AppSettings.PathData</c>
        /// and returns one <see cref="PointViewModel"/> per unique device-point-type.
        /// </summary>
        public List<PointViewModel> LoadAllPointsFromAllDatabases()
        {
            string folder = new SettingsService()
                .LoadSettings()
                .PathData;

            if (string.IsNullOrEmpty(folder))
                folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(folder))
                return new List<PointViewModel>();

            var dbFiles = Directory.GetFiles(folder, "Data_*.db");
            var allPoints = new List<PointViewModel>();

            foreach (string db in dbFiles)
            {
                try
                {
                    using (var conn = new SQLiteConnection("Data Source=" + db + ";"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT * FROM MeasurementData";

                            using (var r = cmd.ExecuteReader())
                            {
                                var rows = new List<MeasurementData>();

                                while (r.Read())
                                {
                                    try
                                    {
                                        rows.Add(new MeasurementData
                                        {
                                            DeviceId = r.GetInt32(r.GetOrdinal("DeviceId")),
                                            PointId = r.GetInt32(r.GetOrdinal("PointId")),
                                            MeasurementId = r.GetInt32(r.GetOrdinal("MeasurementId")),
                                            DeviceName = r["DeviceName"].ToString(),
                                            PointName = r["PointName"].ToString(),
                                            MeasurementName = r["MeasurementName"].ToString(),
                                            Value = r.GetDouble(r.GetOrdinal("Value")),
                                            Unit = r["Unit"].ToString(),
                                            Timestamp = DateTime.Parse(r["Timestamp"].ToString()),
                                            PointType = (PointType)r.GetInt32(r.GetOrdinal("PointType"))
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        LogService.LogError("Row parse error: " + ex.Message);
                                    }
                                }

                                // One PointViewModel per (device, point, type)
                                var grouped = rows
                                    .GroupBy(m => new { m.DeviceId, m.PointId, m.PointType })
                                    .Select(g => new PointViewModel
                                    {
                                        DeviceName = g.First().DeviceName,
                                        PointName = g.First().PointName,
                                        DeviceId = g.Key.DeviceId,
                                        PointId = g.Key.PointId,
                                        Type = g.Key.PointType,
                                        Measurements = new ObservableCollection<MeasurementViewModel>(
                                            g.GroupBy(m => m.MeasurementId)
                                             .Select(mg => mg
                                                 .OrderByDescending(m => m.Timestamp)
                                                 .First())          // latest value per measurement
                                             .Select(m => new MeasurementViewModel
                                             {
                                                 Id = m.MeasurementId,
                                                 Name = m.MeasurementName,
                                                 Unit = m.Unit,
                                                 Value = m.Value,
                                                 ValueStr = m.Value.ToString("F2"),
                                                 Timestamp = m.Timestamp
                                             }))
                                    });

                                allPoints.AddRange(grouped);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError("Failed to read DB file " + db + ": " + ex.Message);
                }
            }

            return allPoints;
        }
    }
}
