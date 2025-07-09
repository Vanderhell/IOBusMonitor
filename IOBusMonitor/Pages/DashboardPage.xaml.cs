using System.Windows.Controls;

namespace IOBusMonitor
{
    /// <summary>
    /// Dashboard – real-time overview of all devices and measurements.
    /// </summary>
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }
    }
}
