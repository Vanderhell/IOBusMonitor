using IOBusMonitorLib;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace IOBusMonitor
{
    /// <summary>
    /// View-model for the real-time dashboard. Groups points by protocol and
    /// updates collections whenever TimerService broadcasts new data.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly TimerService _timerService;

        public ObservableCollection<PointViewModel> ModbusTCPPoints { get; }
            = new ObservableCollection<PointViewModel>();

        public ObservableCollection<PointViewModel> ModbusRTUPoints { get; }
            = new ObservableCollection<PointViewModel>();

        public ObservableCollection<PointViewModel> SiemensPoints { get; }
            = new ObservableCollection<PointViewModel>();

        public DashboardViewModel()
        {
            var mainVm = Application.Current.MainWindow?.DataContext as MainViewModel;
            if (mainVm != null)
            {
                _timerService = mainVm._timerService;
                if (_timerService != null)
                    _timerService.PointRead += OnPointRead;
            }

            LoadPoints();
        }

        /// <summary>
        /// Handler called every time a point is read by TimerService.
        /// Updates or adds the point in the appropriate collection.
        /// </summary>
        private void OnPointRead(PointViewModel newPoint)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var target = GetTargetCollection(newPoint.Type);
                if (target == null) return;

                var existing = target.FirstOrDefault(p =>
                    p.PointId == newPoint.PointId &&
                    p.DeviceId == newPoint.DeviceId);

                if (existing == null)
                {
                    AttachCommands(newPoint);
                    target.Add(newPoint);
                }
                else
                {
                    existing.Measurements.Clear();
                    foreach (var m in newPoint.Measurements)
                    {
                        m.ShowGraphCommand = new RelayCommand(() => ShowGraph(existing, m));
                        existing.Measurements.Add(m);
                    }

                    existing.Timestamp = newPoint.Timestamp;
                    existing.LastScan = newPoint.LastScan;
                }
            });
        }

        /// <summary>
        /// Returns the collection where a point of a given type should be stored.
        /// </summary>
        private ObservableCollection<PointViewModel> GetTargetCollection(PointType type)
        {
            switch (type)
            {
                case PointType.ModbusTCP: return ModbusTCPPoints;
                case PointType.ModbusRTU: return ModbusRTUPoints;
                case PointType.S7: return SiemensPoints;
                default: return null;
            }
        }

        /// <summary>
        /// Loads all points from the database and groups them to avoid duplicates.
        /// </summary>
        public void LoadPoints()
        {
            var loader = new DataLoaderService();
            var loadedPoints = loader.LoadAllPointsFromAllDatabases();

            var grouped = loadedPoints
                .GroupBy(p => new { p.PointId, p.PointName, p.DeviceId, p.Type })
                .Select(g => g.First())
                .ToList();

            ModbusTCPPoints.Clear();
            ModbusRTUPoints.Clear();
            SiemensPoints.Clear();

            foreach (var point in grouped)
            {
                AttachCommands(point);
                var target = GetTargetCollection(point.Type);
                if (target != null) target.Add(point);
            }
        }

        // ---------- Helpers ----------

        private void AttachCommands(PointViewModel point)
        {
            point.ShowAllMeasurementsCommand =
                new RelayCommand(() => ShowGraphAllMeasurements(point));

            foreach (var m in point.Measurements)
            {
                m.ShowGraphCommand =
                    new RelayCommand(() => ShowGraph(point, m));
            }
        }

        private void ShowGraph(PointViewModel point, MeasurementViewModel m)
        {
            var wnd = new GraphWindow(point, m.Id);
            wnd.Show();
        }

        private void ShowGraphAllMeasurements(PointViewModel point)
        {
            var wnd = new GraphWindow(point, showAllMeasurements: true);
            wnd.Show();
        }
    }
}
