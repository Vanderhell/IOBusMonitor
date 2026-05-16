using System.Windows.Controls;

namespace IOBusMonitor
{
    /// <summary>
    /// Simple About page displaying version and app description.
    /// </summary>
    public partial class AboutApp : Page
    {
        public AboutApp()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string AppVersionText
        {
            get { return "Version: " + AppVersionProvider.GetDisplayVersion(); }
        }
    }
}
