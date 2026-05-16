using IOBusMonitorLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IOBusMonitor
{
    /// <summary>
    /// Main application view-model.
    /// Handles navigation, global commands and start/stop of the polling timer.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public readonly TimerService _timerService;
        private readonly Frame _mainFrame;
        private readonly DemoModeService _demoModeService = new DemoModeService();

        public ObservableCollection<PointViewModel> LatestPoints { get; }

        private DateTime? _lastScanUtc;
        private int _activeDevicesCount;
        private string _dataPath;
        private string _shellSubtitle;
        private bool _isDemoModeEnabled;
        private bool _showEnableDemoPrompt;

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand EnableDemoModeCommand { get; }
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ShowModbusTCPDevicesCommand { get; }
        public ICommand ShowModbusTCPPointsCommand { get; }
        public ICommand ShowModbusTCPMeasurementsCommand { get; }
        public ICommand ShowModbusRTUDevicesCommand { get; }
        public ICommand ShowModbusRTUPointsCommand { get; }
        public ICommand ShowModbusRTUMeasurementsCommand { get; }
        public ICommand ShowS7DevicesCommand { get; }
        public ICommand ShowS7PointsCommand { get; }
        public ICommand ShowS7MeasurementsCommand { get; }
        public ICommand ShowAppSettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ShowAboutCommand { get; }

        private bool _isMonitoring;
        public bool IsMonitoring
        {
            get { return _isMonitoring; }
            set
            {
                _isMonitoring = value;
                OnPropertyChanged(nameof(IsMonitoring));
                OnPropertyChanged(nameof(MonitoringStateText));
            }
        }

        public string MonitoringStateText
        {
            get
            {
                if (IsDemoModeEnabled)
                    return IsMonitoring ? "Demo mode active" : "Demo mode ready";
                return IsMonitoring ? "Monitoring active" : "Monitoring stopped";
            }
        }

        public bool IsDemoModeEnabled
        {
            get { return _isDemoModeEnabled; }
            set
            {
                _isDemoModeEnabled = value;
                OnPropertyChanged(nameof(IsDemoModeEnabled));
                OnPropertyChanged(nameof(MonitoringStateText));
                OnPropertyChanged(nameof(DemoBadgeText));
            }
        }

        public string DemoBadgeText
        {
            get { return IsDemoModeEnabled ? "DEMO MODE" : string.Empty; }
        }

        public bool ShowEnableDemoPrompt
        {
            get { return _showEnableDemoPrompt; }
            set { _showEnableDemoPrompt = value; OnPropertyChanged(nameof(ShowEnableDemoPrompt)); }
        }

        public int ActiveDevicesCount
        {
            get { return _activeDevicesCount; }
            set { _activeDevicesCount = value; OnPropertyChanged(nameof(ActiveDevicesCount)); }
        }

        public string LastScanDisplay
        {
            get { return _lastScanUtc.HasValue ? _lastScanUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") : "No successful scan yet"; }
        }

        public string DataPath
        {
            get { return _dataPath; }
            set { _dataPath = value; OnPropertyChanged(nameof(DataPath)); }
        }

        public string AppVersion
        {
            get { return AppVersionProvider.GetDisplayVersion(); }
        }

        public string ShellSubtitle
        {
            get { return _shellSubtitle; }
            set { _shellSubtitle = value; OnPropertyChanged(nameof(ShellSubtitle)); }
        }

        public MainViewModel(Frame mainFrame)
        {
            _mainFrame = mainFrame;
            _timerService = new TimerService();
            _timerService.PointRead += OnPointRead;

            LatestPoints = new ObservableCollection<PointViewModel>();
            ShellSubtitle = "Monitor live traffic, configuration pages, and archive access from one shell.";
            RefreshShellStatus();

            StartCommand = new RelayCommand(StartMonitoring);
            StopCommand = new RelayCommand(StopMonitoring, () => _timerService.IsRunning);
            EnableDemoModeCommand = new RelayCommand(EnableDemoMode);

            ShowDashboardCommand = new RelayCommand(() => NavigateTo(new DashboardPage()));
            ShowHistoryCommand = new RelayCommand(() => NavigateTo(new HistoryPage()));

            ShowModbusTCPDevicesCommand = new RelayCommand(() => NavigateTo(new ModbusTCPDeviceAdminPage()));
            ShowModbusTCPPointsCommand = new RelayCommand(() => NavigateTo(new ModbusTCPPointAdminPage()));
            ShowModbusTCPMeasurementsCommand = new RelayCommand(() => NavigateTo(new ModbusTCPMeasurementAdminPage()));

            ShowModbusRTUDevicesCommand = new RelayCommand(() => NavigateTo(new ModbusRTUDeviceAdminPage()));
            ShowModbusRTUPointsCommand = new RelayCommand(() => NavigateTo(new ModbusRTUPointAdminPage()));
            ShowModbusRTUMeasurementsCommand = new RelayCommand(() => NavigateTo(new ModbusRTUMeasurementAdminPage()));

            ShowS7DevicesCommand = new RelayCommand(() => NavigateTo(new SimensDeviceAdminPage()));
            ShowS7PointsCommand = new RelayCommand(() => NavigateTo(new SimensPointAdminPage()));
            ShowS7MeasurementsCommand = new RelayCommand(() => NavigateTo(new SimensMeasurementAdminPage()));

            ShowAppSettingsCommand = new RelayCommand(() => NavigateTo(new AppSettingsPage()));
            ShowAboutCommand = new RelayCommand(() => NavigateTo(new AboutApp()));

            ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
            RestartCommand = new RelayCommand(ResetSettings);
        }

        private void ResetSettings()
        {
            var settingsService = new SettingsService();
            var settings = settingsService.LoadSettings();
            settingsService.SaveSettings(settings);

            HandyControl.Controls.Growl.SuccessGlobal("Settings were reset to default values.");
            RefreshShellStatus();
        }

        public void StartMonitoring()
        {
            _timerService.ReloadSettings();
            _timerService.Start();
            IsMonitoring = true;
            RefreshShellStatus();
        }

        public void StopMonitoring()
        {
            _timerService.Stop();
            IsMonitoring = false;
            RefreshShellStatus();
        }

        private void NavigateTo(Page page)
        {
            if (_mainFrame != null) _mainFrame.Navigate(page);
        }

        private void OnPointRead(PointViewModel point)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LatestPoints.Add(point);
                if (point.LastSuccessUtc.HasValue)
                    _lastScanUtc = point.LastSuccessUtc;
                RefreshShellStatus();
            });
        }

        private void RefreshShellStatus()
        {
            var settings = new SettingsService().LoadSettings();
            DataPath = string.IsNullOrWhiteSpace(settings.PathData)
                ? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
                : settings.PathData;
            IsDemoModeEnabled = settings.DemoModeEnabled;
            ShowEnableDemoPrompt = !settings.DemoModeEnabled && !_demoModeService.HasAnyConfiguredDevices();

            ActiveDevicesCount = new HashSet<string>(
                _timerService.LivePoints.Select(p => p.Type + "|" + p.DeviceId)).Count;
            OnPropertyChanged(nameof(LastScanDisplay));
        }

        public void RefreshConfiguration()
        {
            bool wasRunning = _timerService.IsRunning;
            if (wasRunning)
                _timerService.Stop();

            _timerService.ReloadSettings();
            IsMonitoring = false;

            if (wasRunning)
                StartMonitoring();
            else
                RefreshShellStatus();
        }

        private void EnableDemoMode()
        {
            var settingsService = new SettingsService();
            var settings = settingsService.LoadSettings();

            _demoModeService.EnsureDemoConfiguration(resetDemoData: false);
            _demoModeService.SetDemoDeviceActiveState(true);
            settings.DemoModeEnabled = true;
            settingsService.SaveSettings(settings);

            RefreshConfiguration();
            NavigateTo(new DashboardPage());
            StartMonitoring();
        }
    }
}
