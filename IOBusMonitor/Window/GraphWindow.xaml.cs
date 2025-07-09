using IOBusMonitorLib;
using System.Windows;
using System.Windows.Controls;

namespace IOBusMonitor
{
    /// <summary>
    /// Graph window – shows a live or historical chart
    /// for one point (all or single measurements).
    /// </summary>
    public partial class GraphWindow : Window
    {
        public GraphViewModel ViewModel { get; }

        /// <summary>
        /// Shows all visible measurements for the point.
        /// </summary>
        public GraphWindow(PointViewModel point, bool showAllMeasurements)
        {
            InitializeComponent();
            ViewModel = new GraphViewModel(point, showAllMeasurements);
            DataContext = ViewModel;
        }

        /// <summary>
        /// Shows only one specific measurement (ID) for the point.
        /// Hides the measurement-list panel.
        /// </summary>
        public GraphWindow(PointViewModel point, int specificMeasurementId)
        {
            InitializeComponent();
            ViewModel = new GraphViewModel(point, specificMeasurementId);
            DataContext = ViewModel;

            // Hide the left panel when only one series is shown
            MeasurementListColumn.Width = new GridLength(0);
            MeasurementListPanel.Visibility = Visibility.Collapsed;
        }

        // Refresh the plot whenever a checkbox toggles
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.UpdatePlot();
        }
    }
}
