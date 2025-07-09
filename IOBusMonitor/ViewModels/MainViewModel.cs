using IOBusMonitorLib;
using System.Collections.ObjectModel;
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
        /// <summary>Background service that periodically reads points.</summary>
        public readonly TimerService _timerService;

        /// <summary>Host frame used for page navigation.</summary>
        private readonly Frame _mainFrame;

        /// <summary>Latest points pushed by TimerService, shown in the UI.</summary>
        public ObservableCollection<PointViewModel> LatestPoints { get; }

        // ---------------- UI commands ----------------
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
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
        /// <summary>True while TimerService is running.</summary>
        public bool IsMonitoring
        {
            get { return _isMonitoring; }
            set { _isMonitoring = value; OnPropertyChanged(nameof(IsMonitoring)); }
        }

        public MainViewModel(Frame mainFrame)
        {
            _mainFrame = mainFrame;
            _timerService = new TimerService();
            _timerService.PointRead += OnPointRead;

            LatestPoints = new ObservableCollection<PointViewModel>();

            // --- monitoring commands ---
            StartCommand = new RelayCommand(StartMonitoring);
            StopCommand = new RelayCommand(StopMonitoring, () => _timerService.IsRunning);

            // --- navigation commands ---
            ShowDashboardCommand = new RelayCommand(() => NavigateTo(new DashboardPage()));
            ShowHistoryCommand = new RelayCommand(() => NavigateTo(new HistoryPage()));

            // Modbus TCP admin pages
            ShowModbusTCPDevicesCommand = new RelayCommand(() => NavigateTo(new ModbusTCPDeviceAdminPage()));
            ShowModbusTCPPointsCommand = new RelayCommand(() => NavigateTo(new ModbusTCPPointAdminPage()));
            ShowModbusTCPMeasurementsCommand = new RelayCommand(() => NavigateTo(new ModbusTCPMeasurementAdminPage()));

            // Modbus RTU admin pages
            ShowModbusRTUDevicesCommand = new RelayCommand(() => NavigateTo(new ModbusRTUDeviceAdminPage()));
            ShowModbusRTUPointsCommand = new RelayCommand(() => NavigateTo(new ModbusRTUPointAdminPage()));
            ShowModbusRTUMeasurementsCommand = new RelayCommand(() => NavigateTo(new ModbusRTUMeasurementAdminPage()));

            // Siemens S7 admin pages
            ShowS7DevicesCommand = new RelayCommand(() => NavigateTo(new SimensDeviceAdminPage()));
            ShowS7PointsCommand = new RelayCommand(() => NavigateTo(new SimensPointAdminPage()));
            ShowS7MeasurementsCommand = new RelayCommand(() => NavigateTo(new SimensMeasurementAdminPage()));

            // Misc pages
            ShowAppSettingsCommand = new RelayCommand(() => NavigateTo(new AppSettingsPage()));
            ShowAboutCommand = new RelayCommand(() => NavigateTo(new AboutApp()));

            // Application control
            ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
            RestartCommand = new RelayCommand(ResetSettings);
        }

        // ---------- settings reset ----------
        private void ResetSettings()
        {
            var settingsService = new SettingsService();
            var settings = settingsService.LoadSettings();  // adjust to defaults if needed
            settingsService.SaveSettings(settings);

            HandyControl.Controls.Growl.SuccessGlobal("Settings were reset to default values.");
        }

        // ---------- monitoring ----------
        public void StartMonitoring()
        {
            _timerService.Start();
            IsMonitoring = true;
        }

        public void StopMonitoring()
        {
            _timerService.Stop();
            IsMonitoring = false;
        }

        // ---------- navigation helper ----------
        private void NavigateTo(Page page)
        {
            if (_mainFrame != null) _mainFrame.Navigate(page);
        }

        // ---------- callback from TimerService ----------
        private void OnPointRead(PointViewModel point)
        {
            Application.Current.Dispatcher.Invoke(() => LatestPoints.Add(point));
        }
    }
}
