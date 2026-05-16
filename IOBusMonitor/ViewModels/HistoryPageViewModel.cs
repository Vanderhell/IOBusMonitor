using IOBusMonitorLib;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace IOBusMonitor
{
    /// <summary>
    /// View-model for the History page – loads historical data and displays them
    /// in an OxyPlot chart. Compatible with C# 7.3.
    /// </summary>
    public class HistoryPageViewModel : ViewModelBase
    {
        private readonly DataLoaderService _dataLoader = new DataLoaderService();

        public ObservableCollection<PointViewModel> AllPoints { get; }
            = new ObservableCollection<PointViewModel>();

        public DateTime RangeStart { get; set; } = DateTime.Now.AddDays(-7);
        public DateTime RangeEnd { get; set; } = DateTime.Now;
        public int HistoryRowLimit { get; set; } = 5000;
        public int ChartPointLimit { get; set; } = 1000;

        private PointViewModel _selectedPoint;
        public PointViewModel SelectedPoint
        {
            get { return _selectedPoint; }
            set
            {
                if (_selectedPoint != value)
                {
                    _selectedPoint = value;
                    OnPropertyChanged(nameof(SelectedPoint));
                }
            }
        }

        public PlotModel PlotModel { get; }
        public ICommand LoadHistoryCommand { get; }

        public HistoryPageViewModel()
        {
            var loadedPoints = _dataLoader.LoadLatestPoints();

            // De-duplicate by PointId + PointName + DeviceId
            var grouped = loadedPoints
                .GroupBy(p => new { p.PointId, p.PointName, p.DeviceId, p.Type })
                .Select(g => g.First())
                .ToList();

            AllPoints.Clear();
            foreach (var p in grouped) AllPoints.Add(p);

            PlotModel = new PlotModel { Title = "Historical Data" };
            LoadHistoryCommand = new RelayCommand(LoadHistoryData);
        }

        // -------------- loading + chart update ----------------

        private void LoadHistoryData()
        {
            if (SelectedPoint == null) return;

            // Load history for this point from every monthly DB
            List<MeasurementViewModel> history = LoadMeasurementHistory(SelectedPoint);

            // Build MeasurementViewModels for checkboxes
            SelectedPoint.Measurements.Clear();
            foreach (var grp in history.GroupBy(h => h.Id))
            {
                var first = grp.First();
                SelectedPoint.Measurements.Add(new MeasurementViewModel
                {
                    Id = first.Id,
                    Name = first.Name,
                    Unit = first.Unit,
                    IsVisible = true
                });
            }

            // Subscribe to IsVisible changes
            foreach (var m in SelectedPoint.Measurements)
            {
                m.PropertyChanged -= Measurement_PropertyChanged;
                m.PropertyChanged += Measurement_PropertyChanged;
            }

            UpdatePlot(history);
        }

        private void Measurement_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MeasurementViewModel.IsVisible) && SelectedPoint != null)
                UpdatePlot(LoadMeasurementHistory(SelectedPoint));
        }

        private void UpdatePlot(List<MeasurementViewModel> history)
        {
            PlotModel.Series.Clear();
            PlotModel.Axes.Clear();

            // Axes
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

            // One series per measurement
            foreach (var grp in history.GroupBy(h => h.Id))
            {
                var measurementVm = SelectedPoint.Measurements
                    .FirstOrDefault(m => m.Id == grp.Key);

                if (measurementVm != null && measurementVm.IsVisible)
                {
                    var series = new LineSeries
                    {
                        Title = measurementVm.Name,
                        MarkerType = MarkerType.Circle
                    };

                    foreach (var m in grp)
                        series.Points.Add(
                            new DataPoint(DateTimeAxis.ToDouble(m.Timestamp), m.Value));

                    PlotModel.Series.Add(series);
                }
            }

            PlotModel.InvalidatePlot(true);
        }

        private List<MeasurementViewModel> LoadMeasurementHistory(PointViewModel point)
        {
            return _dataLoader.LoadMeasurementHistory(new MeasurementQueryOptions
            {
                RangeStart = RangeStart,
                RangeEnd = RangeEnd,
                PointType = point.Type,
                DeviceId = point.DeviceId,
                PointId = point.PointId,
                RowLimit = HistoryRowLimit,
                MaxChartPoints = ChartPointLimit
            });
        }
    }
}
