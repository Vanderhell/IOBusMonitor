using IOBusMonitorLib;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace IOBusMonitor
{
    public class GraphViewModel : ViewModelBase
    {
        public PointViewModel Point { get; }
        public PlotModel PlotModel { get; }

        private readonly bool _showAllMeasurements;
        private readonly int? _specificMeasurementId;
        private readonly SettingsService _settingsService = new SettingsService();
        private DispatcherTimer _liveTimer;

        private bool _isLiveTracking;
        public bool IsLiveTracking
        {
            get { return _isLiveTracking; }
            set
            {
                _isLiveTracking = value;
                OnPropertyChanged();
                UpdateLiveTracking();
            }
        }

        public GraphViewModel(PointViewModel point, bool showAllMeasurements)
        {
            Point = point;
            _showAllMeasurements = showAllMeasurements;
            PlotModel = new PlotModel { Title = $"{point.DeviceName} - {point.PointName}" };

            SubscribeMeasurementChanges();
            UpdatePlot();
        }

        public GraphViewModel(PointViewModel point, int specificMeasurementId)
        {
            Point = point;
            _specificMeasurementId = specificMeasurementId;
            _showAllMeasurements = false;
            PlotModel = new PlotModel { Title = $"{point.DeviceName} - {point.PointName}" };

            SubscribeMeasurementChanges();
            UpdatePlot();
        }

        // ---------------- private helpers ----------------

        private void SubscribeMeasurementChanges()
        {
            foreach (var m in Point.Measurements)
                m.PropertyChanged += Measurement_PropertyChanged;
        }

        private void Measurement_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MeasurementViewModel.IsVisible))
                UpdatePlot();
        }

        public void UpdatePlot()
        {
            if (_specificMeasurementId.HasValue)
                UpdatePlotForSpecificMeasurement();
            else if (_showAllMeasurements)
                UpdatePlotFromDatabase();
            else
                UpdatePlotFromLastMeasurement();
        }

        private void UpdatePlotForSpecificMeasurement()
        {
            PreparePlot();

            var history = LoadMeasurementHistory(Point, _specificMeasurementId.Value);
            var series = new LineSeries
            {
                Title = history.FirstOrDefault()?.Name ?? "Measurement",
                MarkerType = MarkerType.Circle
            };

            foreach (var m in history)
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(m.Timestamp), m.Value));

            PlotModel.Series.Add(series);
            PlotModel.InvalidatePlot(true);
        }

        private void UpdatePlotFromLastMeasurement()
        {
            PreparePlot();

            foreach (var m in Point.Measurements.Where(x => x.IsVisible))
            {
                var series = new LineSeries { Title = m.Name, MarkerType = MarkerType.Circle };
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(m.Timestamp), m.Value));
                PlotModel.Series.Add(series);
            }

            PlotModel.InvalidatePlot(true);
        }

        private void UpdatePlotFromDatabase()
        {
            var history = LoadMeasurementHistory(Point);
            PreparePlot();

            foreach (var group in history.GroupBy(h => h.Id))
            {
                var measurement = group.First();
                if (!Point.Measurements.Any(pm => pm.Id == measurement.Id && pm.IsVisible))
                    continue;

                var series = new LineSeries { Title = measurement.Name, MarkerType = MarkerType.Circle };
                foreach (var m in group)
                    series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(m.Timestamp), m.Value));

                PlotModel.Series.Add(series);
            }

            PlotModel.InvalidatePlot(true);
        }

        private void PreparePlot()
        {
            PlotModel.Series.Clear();
            PlotModel.Axes.Clear();

            PlotModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd.MM.yyyy\nHH:mm:ss",
                Title = "Time",
                IsZoomEnabled = true,
                IsPanEnabled = true
            });

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Value"
            });
        }

        private List<MeasurementViewModel> LoadMeasurementHistory(PointViewModel point, int? filterId = null)
        {
            var history = new List<MeasurementViewModel>();
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(folder)) return history;

            foreach (var dbFile in Directory.GetFiles(folder, "Data_*.db"))
            {
                using (var conn = new SQLiteConnection($"Data Source={dbFile};"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT Timestamp, MeasurementId, MeasurementName, Value, Unit
                                            FROM MeasurementData
                                            WHERE DeviceId = @DeviceId AND PointId = @PointId AND PointType = @PointType
                                            ORDER BY Timestamp";
                        cmd.Parameters.AddWithValue("@DeviceId", point.DeviceId);
                        cmd.Parameters.AddWithValue("@PointId", point.PointId);
                        cmd.Parameters.AddWithValue("@PointType", (int)point.Type);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                var m = new MeasurementViewModel
                                {
                                    Id = r.GetInt32(r.GetOrdinal("MeasurementId")),
                                    Name = r["MeasurementName"].ToString(),
                                    Value = r.GetDouble(r.GetOrdinal("Value")),
                                    Unit = r["Unit"].ToString(),
                                    Timestamp = DateTime.Parse(r["Timestamp"].ToString())
                                };

                                if (!filterId.HasValue || m.Id == filterId.Value)
                                    history.Add(m);
                            }
                        }
                    }
                }
            }
            return history;
        }

        private void UpdateLiveTracking()
        {
            if (_isLiveTracking)
            {
                var settings = _settingsService.LoadSettings();
                int seconds = Math.Max(settings.ReadIntervalMs / 1000, 1);

                _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
                _liveTimer.Tick += (s, e) => UpdatePlot();
                _liveTimer.Start();
            }
            else
            {
                if (_liveTimer != null) _liveTimer.Stop();
                _liveTimer = null;
            }
        }
    }
}
