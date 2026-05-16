using HC = HandyControl.Controls;
using IOBusMonitorLib;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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
                ApplySidebarChrome();
                ApplyWindowStateChrome();

                if (settings.AutoStart)
                {
                    vm.StartMonitoring();
                    vm.ShowDashboardCommand.Execute(null);
                }
                else
                {
                    vm.ShowHomeCommand.Execute(null);
                }
            };

            StateChanged += (s, e) => ApplyWindowStateChrome();

        }

        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsMonitoring))
                UpdateTrayMenuItems();
            else if (e.PropertyName == nameof(MainViewModel.IsSidebarExpanded))
                ApplySidebarChrome();
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

        private void MinimizeWindow_Click(object _, RoutedEventArgs __)
        {
            WindowState = WindowState.Minimized;
        }

        private void ToggleMaximizeWindow_Click(object _, RoutedEventArgs __)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void ShellDragRegion_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximizeWindow_Click(sender, e);
                return;
            }

            if (e.OriginalSource is DependencyObject source && IsInteractiveElement(source))
                return;

            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void SidebarSectionRail_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.IsSidebarExpanded = true;
        }

        private static bool IsInteractiveElement(DependencyObject element)
        {
            while (element != null)
            {
                if (element is ButtonBase)
                    return true;

                element = VisualTreeHelper.GetParent(element);
            }

            return false;
        }

        private void ApplyWindowStateChrome()
        {
            WindowHostBorder.Padding = WindowState == WindowState.Maximized
                ? new Thickness(8)
                : new Thickness(0);
            MaximizeToggleButton.Content = WindowState == WindowState.Maximized
                ? "❐"
                : "□";
        }

        private void ApplySidebarChrome()
        {
            if (SidebarToggleButton == null)
                return;

            SidebarToggleButton.Content = (DataContext as MainViewModel)?.IsSidebarExpanded == true
                ? "‹"
                : "›";
        }
    }
}
