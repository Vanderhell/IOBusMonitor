using HC = HandyControl.Controls;
using IOBusMonitorLib;
using System.ComponentModel;
using System.Windows;

namespace IOBusMonitor
{
    /// <summary>
    /// Main application window – hosts navigation frame and tray logic.
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel(MainContentFrame);
            DataContext = vm;
            vm.PropertyChanged += MainViewModel_PropertyChanged;

            UpdateTrayMenuItems();

            Loaded += (s, e) =>
            {
                var settings = new SettingsService().LoadSettings();

                if (settings.AutoStart)
                {
                    vm.StartMonitoring();
                    MainContentFrame.Content = new DashboardPage();
                }
                else
                {
                    MainContentFrame.Content = new Home();
                }
            };

        }

        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsMonitoring))
                UpdateTrayMenuItems();
        }

        private void UpdateTrayMenuItems()
        {
            var contextMenu = notifyIcon.ContextMenu as System.Windows.Controls.ContextMenu;
            if (contextMenu == null) return;

            var startItem = contextMenu.Items[1] as System.Windows.Controls.MenuItem;
            var stopItem = contextMenu.Items[2] as System.Windows.Controls.MenuItem;
            bool isRunning = (DataContext as MainViewModel)?.IsMonitoring ?? false;

            if (startItem != null) startItem.IsEnabled = !isRunning;
            if (stopItem != null) stopItem.IsEnabled = isRunning;
        }

        private void ShowMainWindow_Click(object _, RoutedEventArgs __)
        {
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;

            Show();
            Activate();
            Topmost = true;
            Topmost = false;
        }

        private void Start_Click(object _, RoutedEventArgs __) =>
            (DataContext as MainViewModel)?.StartCommand.Execute(null);

        private void Stop_Click(object _, RoutedEventArgs __) =>
            (DataContext as MainViewModel)?.StopCommand.Execute(null);

        private void Exit_Click(object _, RoutedEventArgs __)
        {
            var res = MessageBox.Show(this,
                "Are you sure you want to exit?", "Confirm exit",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                notifyIcon.Visibility = Visibility.Collapsed;
                Application.Current.Shutdown();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!Application.Current.ShutdownMode.Equals(ShutdownMode.OnExplicitShutdown))
            {
                e.Cancel = true;
                Hide();
                HC.Growl.InfoGlobal("The application continues to run in the tray.");
            }
            else
            {
                notifyIcon.Visibility = Visibility.Collapsed;
            }
        }

        private void MinimizeToTray_Click(object _, RoutedEventArgs __)
        {
            Hide();
            HC.Growl.InfoGlobal("The application is running in the tray.");
        }
    }
}
