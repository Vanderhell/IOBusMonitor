using HandyControl.Controls;
using IOBusMonitorLib;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace IOBusMonitor
{
    /// <summary>
    /// Application settings page – allows the user to configure read interval,
    /// auto-start and data path. Settings are stored via <see cref="SettingsService"/>.
    /// </summary>
    public partial class AppSettingsPage : Page
    {
        private readonly SettingsService _settingsService = new SettingsService();
        private readonly DemoModeService _demoModeService = new DemoModeService();
        private AppSettings _settings;

        public AppSettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        /// <summary>
        /// Loads settings from the database/file and populates the UI controls.
        /// </summary>
        private void LoadSettings()
        {
            _settings = _settingsService.LoadSettings();

            intervalInput.Value = _settings.ReadIntervalMs;
            autoStartInput.IsChecked = _settings.AutoStart;
            demoModeInput.IsChecked = _settings.DemoModeEnabled;

            pathDataInput.Text = string.IsNullOrWhiteSpace(_settings.PathData)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
                : _settings.PathData;
        }

        /// <summary>
        /// Saves current UI values back to persistent storage.
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _settings.ReadIntervalMs = (int)intervalInput.Value;
            _settings.AutoStart = autoStartInput.IsChecked == true;
            _settings.PathData = string.IsNullOrWhiteSpace(pathDataInput.Text)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
                : pathDataInput.Text.Trim();
            _settings.DemoModeEnabled = demoModeInput.IsChecked == true;

            if (_settings.DemoModeEnabled)
            {
                _demoModeService.EnsureDemoConfiguration(resetDemoData: false);
                _demoModeService.SetDemoDeviceActiveState(true);
            }
            else
            {
                _demoModeService.SetDemoDeviceActiveState(false);
            }

            _settingsService.SaveSettings(_settings);
            RefreshMainShell();
            Growl.Success("Settings saved successfully.");
        }

        private void ResetDemo_Click(object sender, RoutedEventArgs e)
        {
            _settings.PathData = string.IsNullOrWhiteSpace(pathDataInput.Text)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
                : pathDataInput.Text.Trim();
            _demoModeService.ResetDemoDataArchives(_settings.PathData);
            _demoModeService.EnsureDemoConfiguration(resetDemoData: true);
            _demoModeService.SetDemoDeviceActiveState(demoModeInput.IsChecked == true);
            RefreshMainShell();
            Growl.Success("Demo sample configuration and demo history were reset.");
        }

        /// <summary>
        /// Opens a folder browser dialog to select the data directory.
        /// </summary>
        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new Forms.FolderBrowserDialog
            {
                Description = "Select a folder to store application data",
                SelectedPath = pathDataInput.Text
            })
            {
                if (dlg.ShowDialog() == Forms.DialogResult.OK)
                    pathDataInput.Text = dlg.SelectedPath;
            }
        }

        private void RefreshMainShell()
        {
            var vm = System.Windows.Application.Current.MainWindow != null
                ? System.Windows.Application.Current.MainWindow.DataContext as MainViewModel
                : null;

            if (vm != null)
                vm.RefreshConfiguration();
        }
    }
}
