using IOBusMonitorLib;
using System;
using System.Windows.Input;

namespace IOBusMonitor
{
    /// <summary>
    /// Flattened dashboard row for one live measurement.
    /// </summary>
    public class DashboardMeasurementRowViewModel : ViewModelBase
    {
        public PointViewModel Point { get; set; }
        public MeasurementViewModel Measurement { get; set; }
        public string Protocol { get; set; }
        public string Device { get; set; }
        public string PointName { get; set; }
        public string MeasurementName { get; set; }
        public double Value { get; set; }
        public string ValueDisplay { get; set; }
        public string Unit { get; set; }
        public DateTime LastScan { get; set; }
        public string LastScanDisplay { get; set; }
        public PointStatus Status { get; set; }
        public string StatusDisplay { get; set; }
        public string SearchText { get; set; }
        public ICommand ShowGraphCommand { get; set; }
    }
}
