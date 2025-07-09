using IOBusMonitorLib;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
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
        public ObservableCollection<PointViewModel> AllPoints { get; }
            = new ObservableCollection<PointViewModel>();

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
            // Load all points from every device / database
            var loader = new DataLoaderService();
            var loadedPoints = loader.LoadAllPointsFromAllDatabases();

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

        /// <summary>
        /// Loads historical values for a point from every monthly SQLite DB in /Data.
        /// </summary>
        private List<MeasurementViewModel> LoadMeasurementHistory(PointViewModel point)
        {
            var history = new List<MeasurementViewModel>();
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
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
                                            WHERE DeviceId = @DeviceId AND PointId = @PointId
                                            ORDER BY Timestamp";
                        cmd.Parameters.AddWithValue("@DeviceId", point.DeviceId);
                        cmd.Parameters.AddWithValue("@PointId", point.PointId);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                history.Add(new MeasurementViewModel
                                {
                                    Id = r.GetInt32(r.GetOrdinal("MeasurementId")),
                                    Name = r["MeasurementName"].ToString(),
                                    Value = r.GetDouble(r.GetOrdinal("Value")),
                                    Unit = r["Unit"].ToString(),
                                    Timestamp = DateTime.Parse(r["Timestamp"].ToString()),
                                    IsVisible = true
                                });
                            }
                        }
                    }
                }
            }
            return history;
        }
    }
}
