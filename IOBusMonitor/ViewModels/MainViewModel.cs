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
        private string _selectedSidebarSection = "Monitoring";
        private string _selectedNavigationItem;
        private bool _isSidebarExpanded = true;
        private string _currentPageTitle = "Welcome";
        private string _currentPageDescription = "Use the navigation rail to open live monitoring, history, or configuration pages.";
        private string _currentPageBreadcrumb = "Monitoring / Overview";

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand EnableDemoModeCommand { get; }
        public ICommand ShowHomeCommand { get; }
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
                    return IsMonitoring ? "Demo active" : "Demo ready";
                return IsMonitoring ? "Live active" : "Live idle";
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

        public string CurrentPageTitle
        {
            get { return _currentPageTitle; }
            set { SetProperty(ref _currentPageTitle, value); }
        }

        public string CurrentPageDescription
        {
            get { return _currentPageDescription; }
            set { SetProperty(ref _currentPageDescription, value); }
        }

        public string CurrentPageBreadcrumb
        {
            get { return _currentPageBreadcrumb; }
            set { SetProperty(ref _currentPageBreadcrumb, value); }
        }

        public bool IsSidebarExpanded
        {
            get { return _isSidebarExpanded; }
            set { SetProperty(ref _isSidebarExpanded, value); }
        }

        public bool IsHomeSelected
        {
            get { return _selectedNavigationItem == "Home"; }
            set { if (value) SelectNavigationItem("Monitoring", "Home"); }
        }

        public bool IsDashboardSelected
        {
            get { return _selectedNavigationItem == "Dashboard"; }
            set { if (value) SelectNavigationItem("Monitoring", "Dashboard"); }
        }

        public bool IsHistorySelected
        {
            get { return _selectedNavigationItem == "History"; }
            set { if (value) SelectNavigationItem("Monitoring", "History"); }
        }

        public bool IsModbusTcpDevicesSelected
        {
            get { return _selectedNavigationItem == "ModbusTcpDevices"; }
            set { if (value) SelectNavigationItem("ModbusTcp", "ModbusTcpDevices"); }
        }

        public bool IsModbusTcpPointsSelected
        {
            get { return _selectedNavigationItem == "ModbusTcpPoints"; }
            set { if (value) SelectNavigationItem("ModbusTcp", "ModbusTcpPoints"); }
        }

        public bool IsModbusTcpMeasurementsSelected
        {
            get { return _selectedNavigationItem == "ModbusTcpMeasurements"; }
            set { if (value) SelectNavigationItem("ModbusTcp", "ModbusTcpMeasurements"); }
        }

        public bool IsModbusRtuDevicesSelected
        {
            get { return _selectedNavigationItem == "ModbusRtuDevices"; }
            set { if (value) SelectNavigationItem("ModbusRtu", "ModbusRtuDevices"); }
        }

        public bool IsModbusRtuPointsSelected
        {
            get { return _selectedNavigationItem == "ModbusRtuPoints"; }
            set { if (value) SelectNavigationItem("ModbusRtu", "ModbusRtuPoints"); }
        }

        public bool IsModbusRtuMeasurementsSelected
        {
            get { return _selectedNavigationItem == "ModbusRtuMeasurements"; }
            set { if (value) SelectNavigationItem("ModbusRtu", "ModbusRtuMeasurements"); }
        }

        public bool IsS7DevicesSelected
        {
            get { return _selectedNavigationItem == "S7Devices"; }
            set { if (value) SelectNavigationItem("S7", "S7Devices"); }
        }

        public bool IsS7PointsSelected
        {
            get { return _selectedNavigationItem == "S7Points"; }
            set { if (value) SelectNavigationItem("S7", "S7Points"); }
        }

        public bool IsS7MeasurementsSelected
        {
            get { return _selectedNavigationItem == "S7Measurements"; }
            set { if (value) SelectNavigationItem("S7", "S7Measurements"); }
        }

        public bool IsSettingsSelected
        {
            get { return _selectedNavigationItem == "Settings"; }
            set { if (value) SelectNavigationItem("Application", "Settings"); }
        }

        public bool IsResetSettingsSelected
        {
            get { return _selectedNavigationItem == "ResetSettings"; }
            set { if (value) SelectNavigationItem("Application", "ResetSettings"); }
        }

        public bool IsAboutSelected
        {
            get { return _selectedNavigationItem == "About"; }
            set { if (value) SelectNavigationItem("Application", "About"); }
        }

        public bool IsMonitoringSectionSelected
        {
            get { return _selectedSidebarSection == "Monitoring"; }
            set { if (value) SelectSidebarSection("Monitoring"); }
        }

        public bool IsModbusTcpSectionSelected
        {
            get { return _selectedSidebarSection == "ModbusTcp"; }
            set { if (value) SelectSidebarSection("ModbusTcp"); }
        }

        public bool IsModbusRtuSectionSelected
        {
            get { return _selectedSidebarSection == "ModbusRtu"; }
            set { if (value) SelectSidebarSection("ModbusRtu"); }
        }

        public bool IsS7SectionSelected
        {
            get { return _selectedSidebarSection == "S7"; }
            set { if (value) SelectSidebarSection("S7"); }
        }

        public bool IsApplicationSectionSelected
        {
            get { return _selectedSidebarSection == "Application"; }
            set { if (value) SelectSidebarSection("Application"); }
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

            ShowHomeCommand = new RelayCommand(() => NavigateTo(new Home(), "Monitoring", "Home"));
            ShowDashboardCommand = new RelayCommand(() => NavigateTo(new DashboardPage(), "Monitoring", "Dashboard"));
            ShowHistoryCommand = new RelayCommand(() => NavigateTo(new HistoryPage(), "Monitoring", "History"));

            ShowModbusTCPDevicesCommand = new RelayCommand(() => NavigateTo(new ModbusTCPDeviceAdminPage(), "ModbusTcp", "ModbusTcpDevices"));
            ShowModbusTCPPointsCommand = new RelayCommand(() => NavigateTo(new ModbusTCPPointAdminPage(), "ModbusTcp", "ModbusTcpPoints"));
            ShowModbusTCPMeasurementsCommand = new RelayCommand(() => NavigateTo(new ModbusTCPMeasurementAdminPage(), "ModbusTcp", "ModbusTcpMeasurements"));

            ShowModbusRTUDevicesCommand = new RelayCommand(() => NavigateTo(new ModbusRTUDeviceAdminPage(), "ModbusRtu", "ModbusRtuDevices"));
            ShowModbusRTUPointsCommand = new RelayCommand(() => NavigateTo(new ModbusRTUPointAdminPage(), "ModbusRtu", "ModbusRtuPoints"));
            ShowModbusRTUMeasurementsCommand = new RelayCommand(() => NavigateTo(new ModbusRTUMeasurementAdminPage(), "ModbusRtu", "ModbusRtuMeasurements"));

            ShowS7DevicesCommand = new RelayCommand(() => NavigateTo(new SimensDeviceAdminPage(), "S7", "S7Devices"));
            ShowS7PointsCommand = new RelayCommand(() => NavigateTo(new SimensPointAdminPage(), "S7", "S7Points"));
            ShowS7MeasurementsCommand = new RelayCommand(() => NavigateTo(new SimensMeasurementAdminPage(), "S7", "S7Measurements"));

            ShowAppSettingsCommand = new RelayCommand(() => NavigateTo(new AppSettingsPage(), "Application", "Settings"));
            ShowAboutCommand = new RelayCommand(() => NavigateTo(new AboutApp(), "Application", "About"));

            ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
            RestartCommand = new RelayCommand(ResetSettings);
        }

        private void ResetSettings()
        {
            SelectNavigationItem("Application", "ResetSettings");
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

        private void NavigateTo(Page page, string sectionKey = null, string itemKey = null)
        {
            if (!string.IsNullOrWhiteSpace(sectionKey))
                SelectSidebarSection(sectionKey);
            if (!string.IsNullOrWhiteSpace(itemKey))
                SelectNavigationItem(sectionKey, itemKey);

            if (_mainFrame != null) _mainFrame.Navigate(page);
        }

        private void SelectSidebarSection(string sectionKey)
        {
            if (_selectedSidebarSection == sectionKey)
            {
                if (!IsSidebarExpanded)
                    IsSidebarExpanded = true;
                return;
            }

            _selectedSidebarSection = sectionKey;
            if (!IsSidebarExpanded)
                IsSidebarExpanded = true;
            OnPropertyChanged(nameof(IsMonitoringSectionSelected));
            OnPropertyChanged(nameof(IsModbusTcpSectionSelected));
            OnPropertyChanged(nameof(IsModbusRtuSectionSelected));
            OnPropertyChanged(nameof(IsS7SectionSelected));
            OnPropertyChanged(nameof(IsApplicationSectionSelected));
        }

        private void SelectNavigationItem(string sectionKey, string itemKey)
        {
            if (!string.IsNullOrWhiteSpace(sectionKey))
                SelectSidebarSection(sectionKey);

            if (_selectedNavigationItem == itemKey)
                return;

            _selectedNavigationItem = itemKey;
            OnPropertyChanged(nameof(IsHomeSelected));
            OnPropertyChanged(nameof(IsDashboardSelected));
            OnPropertyChanged(nameof(IsHistorySelected));
            OnPropertyChanged(nameof(IsModbusTcpDevicesSelected));
            OnPropertyChanged(nameof(IsModbusTcpPointsSelected));
            OnPropertyChanged(nameof(IsModbusTcpMeasurementsSelected));
            OnPropertyChanged(nameof(IsModbusRtuDevicesSelected));
            OnPropertyChanged(nameof(IsModbusRtuPointsSelected));
            OnPropertyChanged(nameof(IsModbusRtuMeasurementsSelected));
            OnPropertyChanged(nameof(IsS7DevicesSelected));
            OnPropertyChanged(nameof(IsS7PointsSelected));
            OnPropertyChanged(nameof(IsS7MeasurementsSelected));
            OnPropertyChanged(nameof(IsSettingsSelected));
            OnPropertyChanged(nameof(IsResetSettingsSelected));
            OnPropertyChanged(nameof(IsAboutSelected));
            UpdatePageContext(itemKey);
        }

        private void UpdatePageContext(string itemKey)
        {
            switch (itemKey)
            {
                case "Home":
                    CurrentPageTitle = "Overview";
                    CurrentPageDescription = "Start here for orientation, recommended workflow, and a quick summary of the workspace.";
                    CurrentPageBreadcrumb = "Monitoring / Overview";
                    break;
                case "Dashboard":
                    CurrentPageTitle = "Dashboard";
                    CurrentPageDescription = "Inspect live values, protocol status, and recent scan activity in one table.";
                    CurrentPageBreadcrumb = "Monitoring / Dashboard";
                    break;
                case "History":
                    CurrentPageTitle = "History";
                    CurrentPageDescription = "Load archived samples from local SQLite storage and review measurement trends.";
                    CurrentPageBreadcrumb = "Monitoring / History";
                    break;
                case "ModbusTcpDevices":
                    CurrentPageTitle = "Modbus TCP Devices";
                    CurrentPageDescription = "Define TCP endpoints used by live polling and archive capture.";
                    CurrentPageBreadcrumb = "Modbus TCP / Devices";
                    break;
                case "ModbusTcpPoints":
                    CurrentPageTitle = "Modbus TCP Points";
                    CurrentPageDescription = "Map device points and organize logical addresses for TCP measurements.";
                    CurrentPageBreadcrumb = "Modbus TCP / Points";
                    break;
                case "ModbusTcpMeasurements":
                    CurrentPageTitle = "Modbus TCP Measurements";
                    CurrentPageDescription = "Configure individual values, units, formulas, and history behavior for TCP points.";
                    CurrentPageBreadcrumb = "Modbus TCP / Measurements";
                    break;
                case "ModbusRtuDevices":
                    CurrentPageTitle = "Modbus RTU Devices";
                    CurrentPageDescription = "Configure serial devices, slave IDs, and line parameters for RTU polling.";
                    CurrentPageBreadcrumb = "Modbus RTU / Devices";
                    break;
                case "ModbusRtuPoints":
                    CurrentPageTitle = "Modbus RTU Points";
                    CurrentPageDescription = "Model serial point structures and prepare address groups for RTU reads.";
                    CurrentPageBreadcrumb = "Modbus RTU / Points";
                    break;
                case "ModbusRtuMeasurements":
                    CurrentPageTitle = "Modbus RTU Measurements";
                    CurrentPageDescription = "Set up RTU measurement definitions, scaling, and history capture rules.";
                    CurrentPageBreadcrumb = "Modbus RTU / Measurements";
                    break;
                case "S7Devices":
                    CurrentPageTitle = "Siemens S7 Devices";
                    CurrentPageDescription = "Manage PLC endpoints, rack-slot addressing, and connection settings.";
                    CurrentPageBreadcrumb = "Siemens S7 / Devices";
                    break;
                case "S7Points":
                    CurrentPageTitle = "Siemens S7 Points";
                    CurrentPageDescription = "Organize Siemens point blocks and address mappings for monitored values.";
                    CurrentPageBreadcrumb = "Siemens S7 / Points";
                    break;
                case "S7Measurements":
                    CurrentPageTitle = "Siemens S7 Measurements";
                    CurrentPageDescription = "Tune individual S7 values, formatting, units, and storage behavior.";
                    CurrentPageBreadcrumb = "Siemens S7 / Measurements";
                    break;
                case "Settings":
                    CurrentPageTitle = "Application Settings";
                    CurrentPageDescription = "Adjust workspace defaults, paths, polling behavior, and demo mode settings.";
                    CurrentPageBreadcrumb = "Application / Settings";
                    break;
                case "ResetSettings":
                    CurrentPageTitle = "Application Settings";
                    CurrentPageDescription = "Workspace defaults were reset. Review settings before restarting monitoring.";
                    CurrentPageBreadcrumb = "Application / Settings";
                    break;
                case "About":
                    CurrentPageTitle = "About";
                    CurrentPageDescription = "Version, product positioning, supported protocols, and project context.";
                    CurrentPageBreadcrumb = "Application / About";
                    break;
                default:
                    CurrentPageTitle = "Welcome";
                    CurrentPageDescription = "Use the navigation rail to open live monitoring, history, or configuration pages.";
                    CurrentPageBreadcrumb = "Monitoring / Overview";
                    break;
            }
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
