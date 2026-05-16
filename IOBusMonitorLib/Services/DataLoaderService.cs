using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IOBusMonitorLib
{
    public class MeasurementQueryOptions
    {
        public DateTime? RangeStart { get; set; }
        public DateTime? RangeEnd { get; set; }
        public PointType? PointType { get; set; }
        public int? DeviceId { get; set; }
        public int? PointId { get; set; }
        public int? MeasurementId { get; set; }
        public int? RowLimit { get; set; }
        public int? MaxChartPoints { get; set; }
    }

    /// <summary>
    /// Reads daily data files (Data_yyyyMMdd.db) using bounded, filterable queries.
    /// </summary>
    public class DataLoaderService
    {
        private const int DefaultHistoryRowLimit = 5000;
        private const int DefaultChartPointLimit = 1000;

        /// <summary>
        /// Loads the latest known value per measurement for dashboard-style point lists.
        /// </summary>
        public List<PointViewModel> LoadLatestPoints(MeasurementQueryOptions options = null)
        {
            options = options ?? new MeasurementQueryOptions();
            var latestByMeasurement = new Dictionary<string, MeasurementData>();

            foreach (var dbFile in GetArchiveFiles(options.RangeStart, options.RangeEnd).OrderByDescending(p => p))
            {
                try
                {
                    using (var conn = OpenReadConnection(dbFile))
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = BuildLatestValuesQuery();
                        BindCommonFilters(cmd, options);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                MeasurementData row;
                                try
                                {
                                    row = ReadMeasurementData(r);
                                }
                                catch (Exception ex)
                                {
                                    LogService.LogError("Failed to parse latest measurement row from '" + dbFile + "': " + ex.Message);
                                    continue;
                                }

                                string key = BuildMeasurementKey(row.DeviceId, row.PointId, row.MeasurementId, row.PointType);
                                MeasurementData existing;
                                if (!latestByMeasurement.TryGetValue(key, out existing) || row.Timestamp > existing.Timestamp)
                                    latestByMeasurement[key] = row;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError("Failed to read archive '" + dbFile + "' for latest values: " + ex.Message);
                }
            }

            return latestByMeasurement.Values
                .GroupBy(m => new { m.DeviceId, m.PointId, m.PointType })
                .Select(g => new PointViewModel
                {
                    DeviceName = g.First().DeviceName,
                    PointName = g.First().PointName,
                    DeviceId = g.Key.DeviceId,
                    PointId = g.Key.PointId,
                    Type = g.Key.PointType,
                    Measurements = new ObservableCollection<MeasurementViewModel>(
                        g.OrderBy(m => m.MeasurementId)
                         .Select(m => new MeasurementViewModel
                         {
                             Id = m.MeasurementId,
                             Name = m.MeasurementName,
                             Unit = m.Unit,
                             Value = m.Value,
                             ValueStr = m.Value.ToString("F2"),
                             Timestamp = m.Timestamp,
                             IsVisible = true
                         }))
                })
                .OrderBy(p => p.Type)
                .ThenBy(p => p.DeviceName)
                .ThenBy(p => p.PointName)
                .ToList();
        }

        /// <summary>
        /// Backward-compatible alias for the previous broad-load API.
        /// </summary>
        public List<PointViewModel> LoadAllPointsFromAllDatabases()
        {
            return LoadLatestPoints();
        }

        /// <summary>
        /// Loads filtered and bounded history rows for charts.
        /// </summary>
        public List<MeasurementViewModel> LoadMeasurementHistory(MeasurementQueryOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var history = new List<MeasurementViewModel>();
            int rowLimit = options.RowLimit.GetValueOrDefault(DefaultHistoryRowLimit);

            foreach (var dbFile in GetArchiveFiles(options.RangeStart, options.RangeEnd))
            {
                try
                {
                    using (var conn = OpenReadConnection(dbFile))
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = BuildHistoryQuery();
                        BindHistoryFilters(cmd, options, rowLimit);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                try
                                {
                                    history.Add(new MeasurementViewModel
                                    {
                                        Id = r.GetInt32(r.GetOrdinal("MeasurementId")),
                                        Name = r["MeasurementName"].ToString(),
                                        Value = r.GetDouble(r.GetOrdinal("Value")),
                                        Unit = r["Unit"].ToString(),
                                        Timestamp = DateTime.Parse(r["Timestamp"].ToString(), CultureInfo.InvariantCulture),
                                        IsVisible = true
                                    });
                                }
                                catch (Exception ex)
                                {
                                    LogService.LogError("Failed to parse history row from '" + dbFile + "': " + ex.Message);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError("Failed to read archive '" + dbFile + "' for history query: " + ex.Message);
                }
            }

            history = history
                .OrderByDescending(m => m.Timestamp)
                .Take(rowLimit)
                .OrderBy(m => m.Timestamp)
                .ToList();

            return Downsample(history, options.MaxChartPoints.GetValueOrDefault(DefaultChartPointLimit));
        }

        internal static string BuildLatestValuesQuery()
        {
            return
@"SELECT md.Timestamp,
         md.DeviceId,
         md.PointId,
         md.MeasurementId,
         md.DeviceName,
         md.PointName,
         md.MeasurementName,
         md.Value,
         md.Unit,
         md.PointType
  FROM MeasurementData md
  INNER JOIN (
      SELECT DeviceId, PointId, MeasurementId, PointType, MAX(Timestamp) AS MaxTimestamp
      FROM MeasurementData
      WHERE 1 = 1
        AND (@PointType IS NULL OR PointType = @PointType)
        AND (@DeviceId IS NULL OR DeviceId = @DeviceId)
        AND (@PointId IS NULL OR PointId = @PointId)
        AND (@MeasurementId IS NULL OR MeasurementId = @MeasurementId)
        AND (@RangeStart IS NULL OR Timestamp >= @RangeStart)
        AND (@RangeEnd IS NULL OR Timestamp <= @RangeEnd)
      GROUP BY DeviceId, PointId, MeasurementId, PointType
  ) latest
      ON latest.DeviceId = md.DeviceId
     AND latest.PointId = md.PointId
     AND latest.MeasurementId = md.MeasurementId
     AND latest.PointType = md.PointType
     AND latest.MaxTimestamp = md.Timestamp;";
        }

        internal static string BuildHistoryQuery()
        {
            return
@"SELECT Timestamp,
         MeasurementId,
         MeasurementName,
         Value,
         Unit
  FROM (
      SELECT Timestamp,
             MeasurementId,
             MeasurementName,
             Value,
             Unit
      FROM MeasurementData
      WHERE 1 = 1
        AND (@PointType IS NULL OR PointType = @PointType)
        AND (@DeviceId IS NULL OR DeviceId = @DeviceId)
        AND (@PointId IS NULL OR PointId = @PointId)
        AND (@MeasurementId IS NULL OR MeasurementId = @MeasurementId)
        AND (@RangeStart IS NULL OR Timestamp >= @RangeStart)
        AND (@RangeEnd IS NULL OR Timestamp <= @RangeEnd)
      ORDER BY Timestamp DESC
      LIMIT @RowLimit
  )
  ORDER BY Timestamp ASC;";
        }

        private SQLiteConnection OpenReadConnection(string dbFile)
        {
            var conn = new SQLiteConnection("Data Source=" + dbFile + ";Read Only=True;");
            conn.Open();
            return conn;
        }

        private string GetDataFolder()
        {
            string folder = new SettingsService().LoadSettings().PathData;
            if (string.IsNullOrEmpty(folder))
                folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            return folder;
        }

        private IEnumerable<string> GetArchiveFiles(DateTime? rangeStart, DateTime? rangeEnd)
        {
            string folder = GetDataFolder();
            if (!Directory.Exists(folder))
                return Enumerable.Empty<string>();

            var files = Directory.GetFiles(folder, "Data_*.db");
            if (!rangeStart.HasValue && !rangeEnd.HasValue)
                return files.OrderBy(p => p).ToList();

            DateTime effectiveStart = (rangeStart ?? DateTime.MinValue).Date;
            DateTime effectiveEnd = (rangeEnd ?? DateTime.MaxValue).Date;

            return files
                .Select(path => new { Path = path, Date = TryParseArchiveDate(path) })
                .Where(x => x.Date.HasValue)
                .Where(x => x.Date.Value >= effectiveStart && x.Date.Value <= effectiveEnd)
                .OrderBy(x => x.Date.Value)
                .Select(x => x.Path)
                .ToList();
        }

        private static DateTime? TryParseArchiveDate(string dbFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(dbFile);
            if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith("Data_", StringComparison.OrdinalIgnoreCase))
                return null;

            DateTime parsed;
            if (DateTime.TryParseExact(fileName.Substring(5), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed.Date;

            LogService.LogError("Skipped archive with unexpected name format: " + dbFile);
            return null;
        }

        private static void BindCommonFilters(SQLiteCommand cmd, MeasurementQueryOptions options)
        {
            AddNullableIntParameter(cmd, "@PointType", options.PointType.HasValue ? (int?)options.PointType.Value : null);
            AddNullableIntParameter(cmd, "@DeviceId", options.DeviceId);
            AddNullableIntParameter(cmd, "@PointId", options.PointId);
            AddNullableIntParameter(cmd, "@MeasurementId", options.MeasurementId);
            AddNullableDateTimeParameter(cmd, "@RangeStart", options.RangeStart);
            AddNullableDateTimeParameter(cmd, "@RangeEnd", options.RangeEnd);
        }

        private static void BindHistoryFilters(SQLiteCommand cmd, MeasurementQueryOptions options, int rowLimit)
        {
            BindCommonFilters(cmd, options);

            var limitParam = cmd.Parameters.Add("@RowLimit", DbType.Int32);
            limitParam.Value = rowLimit;
        }

        private static void AddNullableIntParameter(SQLiteCommand cmd, string name, int? value)
        {
            var parameter = cmd.Parameters.Add(name, DbType.Int32);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static void AddNullableDateTimeParameter(SQLiteCommand cmd, string name, DateTime? value)
        {
            var parameter = cmd.Parameters.Add(name, DbType.DateTime);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static MeasurementData ReadMeasurementData(SQLiteDataReader r)
        {
            return new MeasurementData
            {
                DeviceId = r.GetInt32(r.GetOrdinal("DeviceId")),
                PointId = r.GetInt32(r.GetOrdinal("PointId")),
                MeasurementId = r.GetInt32(r.GetOrdinal("MeasurementId")),
                DeviceName = r["DeviceName"].ToString(),
                PointName = r["PointName"].ToString(),
                MeasurementName = r["MeasurementName"].ToString(),
                Value = r.GetDouble(r.GetOrdinal("Value")),
                Unit = r["Unit"].ToString(),
                Timestamp = DateTime.Parse(r["Timestamp"].ToString(), CultureInfo.InvariantCulture),
                PointType = (PointType)r.GetInt32(r.GetOrdinal("PointType"))
            };
        }

        private static string BuildMeasurementKey(int deviceId, int pointId, int measurementId, PointType pointType)
        {
            return deviceId + "|" + pointId + "|" + measurementId + "|" + (int)pointType;
        }

        private static List<MeasurementViewModel> Downsample(List<MeasurementViewModel> history, int maxPoints)
        {
            if (history == null || history.Count <= maxPoints || maxPoints <= 0)
                return history ?? new List<MeasurementViewModel>();
            if (maxPoints == 1)
                return new List<MeasurementViewModel> { history[history.Count - 1] };

            var sampled = new List<MeasurementViewModel>(maxPoints);
            double step = (double)(history.Count - 1) / (maxPoints - 1);

            for (int i = 0; i < maxPoints; i++)
            {
                int index = (int)Math.Round(i * step);
                if (index >= history.Count) index = history.Count - 1;
                sampled.Add(history[index]);
            }

            return sampled
                .GroupBy(m => new { m.Id, m.Timestamp, m.Value })
                .Select(g => g.First())
                .OrderBy(m => m.Timestamp)
                .ToList();
        }
    }
}
