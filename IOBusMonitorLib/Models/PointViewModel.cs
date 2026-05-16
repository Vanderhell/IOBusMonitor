using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace IOBusMonitorLib
{
    /// <summary>
    /// UI-model representing one measuring point (device + point address) and its
    /// latest measurements. Implements <see cref="INotifyPropertyChanged"/> so
    /// binding updates propagate automatically.
    /// </summary>
    public class PointViewModel : INotifyPropertyChanged
    {
        // --------------------------------------------------------------------
        // Backing fields
        // --------------------------------------------------------------------
        private string _deviceName;
        private string _pointName;
        private ObservableCollection<MeasurementViewModel> _measurements;
        private DateTime _timestamp;
        private DateTime _timer;
        private DateTime _lastScan;
        private DateTime? _lastSuccessUtc;
        private DateTime? _lastErrorUtc;
        private string _lastErrorMessage;
        private int _consecutiveFailures;
        private PointStatus _status;
        private int _deviceId;
        private int _measurementId;
        private int _pointId;

        // --------------------------------------------------------------------
        // Commands (injected from outside)
        // --------------------------------------------------------------------
        /// <summary>Opens a line chart for a single measurement.</summary>
        public ICommand ShowGraphCommand { get; set; }

        /// <summary>Opens a chart containing all measurements of this point.</summary>
        public ICommand ShowAllMeasurementsCommand { get; set; }

        /// <summary>Protocol family of this point (Modbus TCP, RTU, S7…).</summary>
        public PointType Type { get; set; }

        // --------------------------------------------------------------------
        // Public properties with change notification
        // --------------------------------------------------------------------
        public int DeviceId
        {
            get => _deviceId;
            set { if (_deviceId != value) { _deviceId = value; OnPropertyChanged(); } }
        }

        public int MeasurementId
        {
            get => _measurementId;
            set { if (_measurementId != value) { _measurementId = value; OnPropertyChanged(); } }
        }

        public int PointId
        {
            get => _pointId;
            set { if (_pointId != value) { _pointId = value; OnPropertyChanged(); } }
        }

        public string DeviceName
        {
            get => _deviceName;
            set { if (_deviceName != value) { _deviceName = value; OnPropertyChanged(); } }
        }

        public string PointName
        {
            get => _pointName;
            set { if (_pointName != value) { _pointName = value; OnPropertyChanged(); } }
        }

        /// <summary>Latest values for this point (one item per measurement).</summary>
        public ObservableCollection<MeasurementViewModel> Measurements
        {
            get => _measurements;
            set { if (_measurements != value) { _measurements = value; OnPropertyChanged(); } }
        }

        /// <summary>Date/time when the data in <see cref="Measurements"/> was read.</summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set { if (_timestamp != value) { _timestamp = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Additional timer value used by the UI (e.g. to show elapsed time
        /// since last scan). Optional – may stay <see cref="DateTime.MinValue"/>.
        /// </summary>
        public DateTime Timer
        {
            get => _timer;
            set { if (_timer != value) { _timer = value; OnPropertyChanged(); } }
        }

        /// <summary>Moment when this point was last polled successfully.</summary>
        public DateTime LastScan
        {
            get => _lastScan;
            set { if (_lastScan != value) { _lastScan = value; OnPropertyChanged(); } }
        }

        /// <summary>UTC timestamp of the last successful communication.</summary>
        public DateTime? LastSuccessUtc
        {
            get => _lastSuccessUtc;
            set { if (_lastSuccessUtc != value) { _lastSuccessUtc = value; OnPropertyChanged(); } }
        }

        /// <summary>UTC timestamp of the last communication error.</summary>
        public DateTime? LastErrorUtc
        {
            get => _lastErrorUtc;
            set { if (_lastErrorUtc != value) { _lastErrorUtc = value; OnPropertyChanged(); } }
        }

        /// <summary>Last known communication error message.</summary>
        public string LastErrorMessage
        {
            get => _lastErrorMessage;
            set { if (_lastErrorMessage != value) { _lastErrorMessage = value; OnPropertyChanged(); } }
        }

        /// <summary>Number of consecutive polling failures for this point/device path.</summary>
        public int ConsecutiveFailures
        {
            get => _consecutiveFailures;
            set { if (_consecutiveFailures != value) { _consecutiveFailures = value; OnPropertyChanged(); } }
        }

        /// <summary>Current communication status for the point/device path.</summary>
        public PointStatus Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        // --------------------------------------------------------------------
        // INotifyPropertyChanged implementation
        // --------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(prop));
        }
    }
}
