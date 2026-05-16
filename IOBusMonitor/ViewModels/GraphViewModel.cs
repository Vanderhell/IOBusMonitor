using IOBusMonitorLib;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly DataLoaderService _dataLoader = new DataLoaderService();
        private DispatcherTimer _liveTimer;
        private readonly DateTime _rangeStart = DateTime.Now.AddDays(-7);
        private readonly DateTime _rangeEnd = DateTime.Now;
        private const int HistoryRowLimit = 5000;
        private const int ChartPointLimit = 1000;

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
            return _dataLoader.LoadMeasurementHistory(new MeasurementQueryOptions
            {
                RangeStart = _rangeStart,
                RangeEnd = _rangeEnd,
                PointType = point.Type,
                DeviceId = point.DeviceId,
                PointId = point.PointId,
                MeasurementId = filterId,
                RowLimit = HistoryRowLimit,
                MaxChartPoints = ChartPointLimit
            });
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
