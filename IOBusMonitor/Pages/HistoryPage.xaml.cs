using System.Windows.Controls;
using System.Windows;

namespace IOBusMonitor
{
    /// <summary>
    /// HistoryPage – displays historical data for selected points in a plot.
    /// </summary>
    public partial class HistoryPage : Page
    {
        private const double ResponsiveBreakpoint = 1080;

        public HistoryPage()
        {
            InitializeComponent();
            DataContext = new HistoryPageViewModel();
            Loaded += HistoryPage_Loaded;
            SizeChanged += HistoryPage_SizeChanged;
        }

        private void HistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyResponsiveLayout(ActualWidth);
        }

        private void HistoryPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.NewSize.Width);
        }

        private void ApplyResponsiveLayout(double width)
        {
            if (width < ResponsiveBreakpoint)
            {
                HistorySidebarColumn.Width = new GridLength(1, GridUnitType.Auto);
                HistoryGapColumn.Width = new GridLength(0);
                HistoryLayoutRoot.RowDefinitions[0].Height = GridLength.Auto;
                HistoryLayoutRoot.RowDefinitions[1].Height = new GridLength(16);
                HistoryLayoutRoot.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                Grid.SetRow(HistoryControlsCard, 0);
                Grid.SetColumn(HistoryControlsCard, 0);
                Grid.SetColumnSpan(HistoryControlsCard, 3);
                Grid.SetRow(HistoryChartCard, 2);
                Grid.SetColumn(HistoryChartCard, 0);
                Grid.SetColumnSpan(HistoryChartCard, 3);
            }
            else
            {
                HistorySidebarColumn.Width = new GridLength(320);
                HistoryGapColumn.Width = new GridLength(16);
                HistoryLayoutRoot.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                HistoryLayoutRoot.RowDefinitions[1].Height = new GridLength(16);
                HistoryLayoutRoot.RowDefinitions[2].Height = GridLength.Auto;
                Grid.SetRow(HistoryControlsCard, 0);
                Grid.SetColumn(HistoryControlsCard, 0);
                Grid.SetColumnSpan(HistoryControlsCard, 1);
                Grid.SetRow(HistoryChartCard, 0);
                Grid.SetColumn(HistoryChartCard, 2);
                Grid.SetColumnSpan(HistoryChartCard, 1);
            }
        }
    }
}
