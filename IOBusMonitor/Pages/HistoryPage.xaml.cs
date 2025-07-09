using System.Windows.Controls;

namespace IOBusMonitor
{
    /// <summary>
    /// HistoryPage – displays historical data for selected points in a plot.
    /// </summary>
    public partial class HistoryPage : Page
    {
        public HistoryPage()
        {
            InitializeComponent();
            DataContext = new HistoryPageViewModel(); // set view-model
        }
    }
}
