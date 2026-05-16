using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace IOBusMonitorLib.Tests
{
    [TestClass]
    public class DataStorageAndLoaderTests
    {
        private readonly SettingsService _settingsService = new SettingsService();
        private string _tempDataFolder;
        private AppSettings _originalSettings;

        [TestInitialize]
        public void TestInitialize()
        {
            _originalSettings = _settingsService.LoadSettings();
            _tempDataFolder = Path.Combine(Path.GetTempPath(), "IOBusMonitorTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDataFolder);

            _settingsService.SaveSettings(new AppSettings
            {
                ReadIntervalMs = _originalSettings.ReadIntervalMs,
                AutoStart = _originalSettings.AutoStart,
                PathData = _tempDataFolder
            });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _settingsService.SaveSettings(new AppSettings
            {
                ReadIntervalMs = _originalSettings.ReadIntervalMs,
                AutoStart = _originalSettings.AutoStart,
                PathData = _originalSettings.PathData
            });

            if (Directory.Exists(_tempDataFolder))
                Directory.Delete(_tempDataFolder, true);
        }

        [TestMethod]
        public void SaveModbusTcpData_CreatesDatabaseSchemaAndIndexes()
        {
            var service = new DataStorageService();
            service.SaveModbusTCPData(CreatePoint(PointType.ModbusTCP, 10, 20, new[] { 1 }, new[] { 25.5 }));

            string dbFile = GetExpectedDbFile();
            Assert.IsTrue(File.Exists(dbFile));

            using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
            {
                conn.Open();

                Assert.AreEqual(1L, ExecuteScalar<long>(conn, "SELECT COUNT(*) FROM MeasurementData"));
                Assert.AreEqual(1L, ExecuteScalar<long>(conn, "PRAGMA user_version"));
                Assert.IsTrue(IndexExists(conn, "IX_MeasurementData_Timestamp"));
                Assert.IsTrue(IndexExists(conn, "IX_MeasurementData_PointHistory"));
                Assert.IsTrue(IndexExists(conn, "IX_MeasurementData_MeasurementHistory"));
            }
        }

        [TestMethod]
        public void SaveModbusRtuData_PersistsModbusRtuPointType()
        {
            var service = new DataStorageService();
            service.SaveModbusRTUData(CreatePoint(PointType.ModbusRTU, 11, 21, new[] { 2 }, new[] { 13.75 }));

            using (var conn = new SQLiteConnection("Data Source=" + GetExpectedDbFile() + ";"))
            {
                conn.Open();
                int pointType = ExecuteScalar<int>(conn, "SELECT PointType FROM MeasurementData LIMIT 1");
                Assert.AreEqual((int)PointType.ModbusRTU, pointType);
            }
        }

        [TestMethod]
        public void LoadLatestPoints_ReturnsLatestValuePerMeasurement()
        {
            var service = new DataStorageService();
            var point = CreatePoint(PointType.ModbusTCP, 12, 22, new[] { 1, 2 }, new[] { 10.0, 20.0 });
            point.Measurements[0].Timestamp = DateTime.Today.AddHours(1);
            point.Measurements[1].Timestamp = DateTime.Today.AddHours(1);
            service.SaveModbusTCPData(point);

            point.Measurements[0].Value = 15.0;
            point.Measurements[0].ValueStr = "15.00";
            point.Measurements[0].Timestamp = DateTime.Today.AddHours(2);
            point.Measurements[1].Timestamp = DateTime.Today.AddHours(1);
            service.SaveModbusTCPData(point);

            var loader = new DataLoaderService();
            List<PointViewModel> latestPoints = loader.LoadLatestPoints(new MeasurementQueryOptions
            {
                DeviceId = 12,
                PointId = 22
            });

            Assert.AreEqual(1, latestPoints.Count);
            Assert.AreEqual(2, latestPoints[0].Measurements.Count);
            Assert.AreEqual(15.0, latestPoints[0].Measurements.Single(m => m.Id == 1).Value, 0.0001);
        }

        [TestMethod]
        public void LoadMeasurementHistory_AppliesMeasurementFilterAndDownsampling()
        {
            var service = new DataStorageService();
            for (int i = 0; i < 10; i++)
            {
                var point = CreatePoint(PointType.ModbusTCP, 13, 23, new[] { 1, 2 }, new[] { (double)i, (double)(i + 100) });
                point.Measurements[0].Timestamp = DateTime.Today.AddMinutes(i);
                point.Measurements[1].Timestamp = DateTime.Today.AddMinutes(i);
                service.SaveModbusTCPData(point);
            }

            var loader = new DataLoaderService();
            List<MeasurementViewModel> history = loader.LoadMeasurementHistory(new MeasurementQueryOptions
            {
                DeviceId = 13,
                PointId = 23,
                PointType = PointType.ModbusTCP,
                MeasurementId = 1,
                RangeStart = DateTime.Today,
                RangeEnd = DateTime.Today.AddHours(1),
                RowLimit = 100,
                MaxChartPoints = 4
            });

            Assert.AreEqual(4, history.Count);
            Assert.IsTrue(history.All(m => m.Id == 1));
            Assert.IsTrue(history.SequenceEqual(history.OrderBy(m => m.Timestamp)));
        }

        [TestMethod]
        public void LoadLatestPoints_WithCorruptArchive_LogsAndContinues()
        {
            var service = new DataStorageService();
            service.SaveSimensData(CreatePoint(PointType.S7, 14, 24, new[] { 1 }, new[] { 42.0 }));

            File.WriteAllText(Path.Combine(_tempDataFolder, "Data_19990101.db"), "not a sqlite database");

            var loader = new DataLoaderService();
            List<PointViewModel> latestPoints = loader.LoadLatestPoints();

            Assert.AreEqual(1, latestPoints.Count);
            Assert.AreEqual(PointType.S7, latestPoints[0].Type);
        }

        [TestMethod]
        public void BuildHistoryQuery_ContainsRangeAndLimitClauses()
        {
            string sql = DataLoaderService.BuildHistoryQuery();

            StringAssert.Contains(sql, "@RangeStart");
            StringAssert.Contains(sql, "@RangeEnd");
            StringAssert.Contains(sql, "LIMIT @RowLimit");
        }

        [TestMethod]
        public void BuildLatestValuesQuery_ContainsLatestTimestampGrouping()
        {
            string sql = DataLoaderService.BuildLatestValuesQuery();

            StringAssert.Contains(sql, "MAX(Timestamp)");
            StringAssert.Contains(sql, "GROUP BY DeviceId, PointId, MeasurementId, PointType");
        }

        private string GetExpectedDbFile()
        {
            return Path.Combine(_tempDataFolder, "Data_" + DateTime.Now.ToString("yyyyMMdd") + ".db");
        }

        private static T ExecuteScalar<T>(SQLiteConnection conn, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                return (T)Convert.ChangeType(cmd.ExecuteScalar(), typeof(T));
            }
        }

        private static bool IndexExists(SQLiteConnection conn, string indexName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = @Name";
                cmd.Parameters.AddWithValue("@Name", indexName);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static PointViewModel CreatePoint(PointType type, int deviceId, int pointId, int[] measurementIds, double[] values)
        {
            var point = new PointViewModel
            {
                DeviceId = deviceId,
                DeviceName = "Device_" + deviceId,
                PointId = pointId,
                PointName = "Point_" + pointId,
                Type = type,
                Measurements = new System.Collections.ObjectModel.ObservableCollection<MeasurementViewModel>()
            };

            for (int i = 0; i < measurementIds.Length; i++)
            {
                point.Measurements.Add(new MeasurementViewModel
                {
                    Id = measurementIds[i],
                    Name = "Measurement_" + measurementIds[i],
                    Unit = "u",
                    Value = values[i],
                    ValueStr = values[i].ToString("F2"),
                    Timestamp = DateTime.Now
                });
            }

            return point;
        }
    }
}
