using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace IOBusMonitor
{
    /// <summary>
    /// Application entry-point; performs single-instance enforcement,
    /// database initialization, and splash-screen startup sequence.
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Force UI culture to en-US.
            System.Threading.Thread.CurrentThread.CurrentUICulture =
                new System.Globalization.CultureInfo("en-US");

            // Ensure only one instance is running; close any previous.
            InstanceManager.EnsureSingleInstanceByClosingPrevious();

            try
            {
                EnsureDatabaseFolderExists();
                CreateSchema();
            }
            catch (Exception ex)
            {
                File.WriteAllText("init_error.log", ex.ToString());
                MessageBox.Show("Database initialization error:\n" + ex.Message,
                                "IOBusMonitor", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Show splash screen while heavy initialization runs.
            var splash = new SplashScreen();
            splash.Show();

            Task.Run(async () =>
            {
                await Task.Delay(2000);            // Simulated work
                Dispatcher.Invoke(() =>
                {
                    MainWindow = new MainWindow();
                    MainWindow.Show();
                    splash.Close();
                });
            });

            base.OnStartup(e);
        }

        /// <summary>
        /// Creates the <c>Settings</c> folder if it does not yet exist.
        /// </summary>
        private void EnsureDatabaseFolderExists()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        /// <summary>
        /// Creates the SQLite schema (if missing) in <c>Settings/Settings.db</c>.
        /// </summary>
        private void CreateSchema()
        {
            string dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                         "Settings", "Settings.db");

            if (!File.Exists(dbFile))
                SQLiteConnection.CreateFile(dbFile);

            using (var connection = new SQLiteConnection($"Data Source={dbFile};"))
            {
                connection.Open();

                // All CREATE TABLE statements
                string[] commands =
                {
                    // AppSettings
                    @"CREATE TABLE IF NOT EXISTS AppSettings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ReadIntervalMs INTEGER,
                        AutoStart BOOLEAN,
                        PathData TEXT
                    )",

                    // Modbus RTU
                    @"CREATE TABLE IF NOT EXISTS ModbusRTUDevice (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        SerialPort INTEGER,
                        BaudRate INTEGER,
                        Parity INTEGER,
                        SlaveId INTEGER,
                        Active BOOLEAN
                    )",
                    @"CREATE TABLE IF NOT EXISTS ModbusRTUPoint (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        ModbusRTUDeviceId INTEGER
                    )",
                    @"CREATE TABLE IF NOT EXISTS RTUMeasurement (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        Unit TEXT,
                        Round INTEGER,
                        Condition TEXT,
                        Register INTEGER,
                        Quantity INTEGER,
                        Active BOOLEAN,
                        BitOrder INTEGER,
                        ModbusRTUPointId INTEGER
                    )",

                    // Modbus TCP
                    @"CREATE TABLE IF NOT EXISTS ModbusTCPDevice (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        IPAddress TEXT,
                        Port INTEGER,
                        Active BOOLEAN
                    )",
                    @"CREATE TABLE IF NOT EXISTS ModbusTCPPoint (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        ModbusTCPDeviceId INTEGER
                    )",
                    @"CREATE TABLE IF NOT EXISTS TCPMeasurement (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        Unit TEXT,
                        Round INTEGER,
                        Condition TEXT,
                        Register INTEGER,
                        Quantity INTEGER,
                        Active BOOLEAN,
                        BitOrder INTEGER,
                        ModbusTCPPointId INTEGER
                    )",

                    // Siemens S7
                    @"CREATE TABLE IF NOT EXISTS SimensDevice (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        IPAddress TEXT,
                        Port INTEGER,
                        Rack INTEGER,
                        Slot INTEGER,
                        CpuType INTEGER,
                        Active BOOLEAN
                    )",
                    @"CREATE TABLE IF NOT EXISTS SimensPoint (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        SimenseDeviceId INTEGER
                    )",
                    @"CREATE TABLE IF NOT EXISTS SimensMeasurement (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        Unit TEXT,
                        Round INTEGER,
                        Condition TEXT,
                        Address TEXT,
                        SimensPointId INTEGER,
                        Active BOOLEAN
                    )"
                };

                // Execute each CREATE TABLE command
                foreach (string cmdText in commands)
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = cmdText;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
