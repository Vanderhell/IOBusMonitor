using IOBusMonitorLib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace IOBusMonitor
{
    /// <summary>
    /// View-model for the live dashboard using a flattened, filterable row model.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly TimerService _timerService;
        private readonly MainViewModel _mainVm;

        public ObservableCollection<DashboardMeasurementRowViewModel> Rows { get; }
            = new ObservableCollection<DashboardMeasurementRowViewModel>();

        public ICollectionView FilteredRows { get; }

        private string _searchText;
        private string _selectedProtocolFilter = "All";
        private string _selectedStatusFilter = "All";

        public IReadOnlyList<string> ProtocolFilters { get; } =
            new[] { "All", "Modbus TCP", "Modbus RTU", "Siemens S7" };

        public IReadOnlyList<string> StatusFilters { get; } =
            new[] { "All", "Online", "Connecting", "Timeout", "ReadError", "Disabled", "Offline", "Unknown" };

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilteredRows.Refresh();
                    NotifyEmptyState();
                }
            }
        }

        public string SelectedProtocolFilter
        {
            get { return _selectedProtocolFilter; }
            set
            {
                if (SetProperty(ref _selectedProtocolFilter, value))
                {
                    FilteredRows.Refresh();
                    NotifyEmptyState();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get { return _selectedStatusFilter; }
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    FilteredRows.Refresh();
                    NotifyEmptyState();
                }
            }
        }

        public bool HasRows
        {
            get { return Rows.Count > 0; }
        }

        private bool IsMonitoringActive
        {
            get { return _mainVm != null && _mainVm.IsMonitoring; }
        }

        public string EmptyStateTitle
        {
            get
            {
                if (Rows.Count > 0)
                    return "No rows match the current filters";
                if (!IsMonitoringActive)
                    return "Monitoring is stopped";
                return "No configured points";
            }
        }

        public string EmptyStateMessage
        {
            get
            {
                if (Rows.Count > 0)
                    return "Adjust the search text or quick filters to broaden the result set.";
                if (!IsMonitoringActive)
                    return "Start monitoring to populate live measurements.";
                return "Configure devices, points, and measurements, then start monitoring.";
            }
        }

        public DashboardViewModel()
        {
            _mainVm = Application.Current.MainWindow?.DataContext as MainViewModel;
            if (_mainVm != null)
            {
                _timerService = _mainVm._timerService;
                if (_timerService != null)
                    _timerService.PointRead += OnPointRead;
                _mainVm.PropertyChanged += MainVm_PropertyChanged;
            }

            FilteredRows = CollectionViewSource.GetDefaultView(Rows);
            FilteredRows.Filter = FilterRow;

            LoadPoints();
        }

        private void MainVm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsMonitoring))
            {
                OnPropertyChanged(nameof(EmptyStateTitle));
                OnPropertyChanged(nameof(EmptyStateMessage));
            }
        }

        private void OnPointRead(PointViewModel newPoint)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpsertRows(newPoint);
                FilteredRows.Refresh();
                NotifyEmptyState();
            });
        }

        public void LoadPoints()
        {
            var loader = new DataLoaderService();
            var loadedPoints = loader.LoadLatestPoints();

            Rows.Clear();
            foreach (var point in loadedPoints)
                AddRows(point);

            FilteredRows.Refresh();
            NotifyEmptyState();
        }

        private void AddRows(PointViewModel point)
        {
            AttachCommands(point);
            foreach (var measurement in point.Measurements ?? Enumerable.Empty<MeasurementViewModel>())
                Rows.Add(BuildRow(point, measurement));
        }

        private void UpsertRows(PointViewModel point)
        {
            var existingRows = Rows
                .Where(r => r.Point.PointId == point.PointId &&
                            r.Point.DeviceId == point.DeviceId &&
                            r.Point.Type == point.Type)
                .ToList();

            foreach (var row in existingRows)
                Rows.Remove(row);

            AddRows(point);
        }

        private bool FilterRow(object item)
        {
            var row = item as DashboardMeasurementRowViewModel;
            if (row == null) return false;

            if (!string.IsNullOrWhiteSpace(SelectedProtocolFilter) &&
                SelectedProtocolFilter != "All" &&
                row.Protocol != SelectedProtocolFilter)
                return false;

            if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) &&
                SelectedStatusFilter != "All" &&
                row.StatusDisplay != SelectedStatusFilter)
                return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(row.SearchText) || !row.SearchText.Contains(search))
                    return false;
            }

            return true;
        }

        private void NotifyEmptyState()
        {
            OnPropertyChanged(nameof(HasRows));
            OnPropertyChanged(nameof(EmptyStateTitle));
            OnPropertyChanged(nameof(EmptyStateMessage));
        }

        private void AttachCommands(PointViewModel point)
        {
            point.ShowAllMeasurementsCommand = new RelayCommand(() => ShowGraphAllMeasurements(point));

            foreach (var m in point.Measurements ?? Enumerable.Empty<MeasurementViewModel>())
                m.ShowGraphCommand = new RelayCommand(() => ShowGraph(point, m));
        }

        private DashboardMeasurementRowViewModel BuildRow(PointViewModel point, MeasurementViewModel measurement)
        {
            string protocol = MapProtocol(point.Type);
            string status = point.Status.ToString();
            string valueDisplay = !string.IsNullOrWhiteSpace(measurement.ValueStr)
                ? measurement.ValueStr
                : measurement.Value.ToString("F2");

            return new DashboardMeasurementRowViewModel
            {
                Point = point,
                Measurement = measurement,
                Protocol = protocol,
                Device = point.DeviceName,
                PointName = point.PointName,
                MeasurementName = measurement.Name,
                Value = measurement.Value,
                ValueDisplay = valueDisplay,
                Unit = measurement.Unit,
                LastScan = point.LastScan,
                LastScanDisplay = point.LastScan == default(System.DateTime)
                    ? "No scan"
                    : point.LastScan.ToString("dd.MM.yyyy HH:mm:ss"),
                Status = point.Status,
                StatusDisplay = status,
                SearchText = (protocol + " " + point.DeviceName + " " + point.PointName + " " +
                              measurement.Name + " " + measurement.Unit + " " + status + " " +
                              (point.LastErrorMessage ?? string.Empty)).ToLowerInvariant(),
                ShowGraphCommand = new RelayCommand(() => ShowGraph(point, measurement))
            };
        }

        private static string MapProtocol(PointType type)
        {
            switch (type)
            {
                case PointType.ModbusTCP: return "Modbus TCP";
                case PointType.ModbusRTU: return "Modbus RTU";
                case PointType.S7: return "Siemens S7";
                default: return "Unknown";
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
