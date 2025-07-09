using HandyControl.Controls;
using IOBusMonitorLib;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace IOBusMonitor
{
    /// <summary>
    /// Application settings page – allows the user to configure read interval,
    /// auto-start and data path. Settings are stored via <see cref="SettingsService"/>.
    /// </summary>
    public partial class AppSettingsPage : Page
    {
        private readonly SettingsService _settingsService = new SettingsService();
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
            _settings.PathData = pathDataInput.Text.Trim();

            _settingsService.SaveSettings(_settings);
            Growl.Success("Settings saved successfully.");
        }

        /// <summary>
        /// Opens a folder browser dialog to select the data directory.
        /// </summary>
        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description = "Select a folder to store application data",
                SelectedPath = pathDataInput.Text
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    pathDataInput.Text = dlg.SelectedPath;
            }
        }
    }
}
