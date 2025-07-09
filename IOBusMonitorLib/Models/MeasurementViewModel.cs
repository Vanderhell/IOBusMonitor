using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Represents one scalar measurement (e.g.&nbsp;temperature, pressure) that
    /// belongs to a particular point. Implements <see cref="INotifyPropertyChanged"/>
    /// so UI bindings update automatically.
    /// </summary>
    public class MeasurementViewModel : INotifyPropertyChanged
    {
        private bool _isVisible = true;

        /// <summary>Database key of the measurement.</summary>
        public int Id { get; set; }

        /// <summary>Human-readable measurement name (e.g. “Temperature”).</summary>
        public string Name { get; set; }

        /// <summary>Physical unit (°C, kPa, …).</summary>
        public string Unit { get; set; }

        /// <summary>Numeric value after rounding / conversion.</summary>
        public double Value { get; set; }

        /// <summary>Value formatted as a string with the required precision.</summary>
        public string ValueStr { get; set; }

        /// <summary>Date-time when the value was acquired.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Command that opens a chart focused on this measurement; injected by
        /// the parent view-model.
        /// </summary>
        public ICommand ShowGraphCommand { get; set; }

        /// <summary>
        /// Flag used by the UI to hide or show this series in charts.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
