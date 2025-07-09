using HandyControl.Controls;
using IOBusMonitorLib;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace IOBusMonitor
{
    /// <summary>
    /// Interaction logic for ModbusRTUDeviceAdminPage.xaml
    /// Handles CRUD operations for Modbus RTU devices using SQLite.
    /// </summary>
    public partial class ModbusRTUDeviceAdminPage : Page
    {
        private List<ModbusRTUDevice> _devices;
        private readonly string _dbFile;

        public ModbusRTUDeviceAdminPage()
        {
            InitializeComponent();
            _dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "Settings.db");

            try
            {
                LoadDevices();
                userGrid.ItemsSource = _devices;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads all Modbus RTU devices from the database.
        /// </summary>
        private void LoadDevices()
        {
            _devices = new List<ModbusRTUDevice>();

            if (!File.Exists(_dbFile))
                return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ModbusRTUDevice";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _devices.Add(new ModbusRTUDevice
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader["Name"].ToString(),
                                Active = reader.GetInt32(reader.GetOrdinal("Active")) == 1,
                                SerialPort = (SerialPortName)Enum.Parse(typeof(SerialPortName), reader["SerialPort"].ToString()),
                                BaudRate = (BaudRate)Enum.Parse(typeof(BaudRate), reader["BaudRate"].ToString()),
                                Parity = (SerialParity)Enum.Parse(typeof(SerialParity), reader["Parity"].ToString()),
                                SlaveId = reader.GetInt32(reader.GetOrdinal("SlaveId"))
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves a Modbus RTU device to the database (insert or update).
        /// </summary>
        private void SaveDevice(ModbusRTUDevice device)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    if (device.Id == 0)
                    {
                        cmd.CommandText = @"
                            INSERT INTO ModbusRTUDevice (Name, SerialPort, BaudRate, Parity, SlaveId, Active)
                            VALUES (@Name, @SerialPort, @BaudRate, @Parity, @SlaveId, @Active)";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE ModbusRTUDevice
                            SET Name=@Name, SerialPort=@SerialPort, BaudRate=@BaudRate,
                                Parity=@Parity, SlaveId=@SlaveId, Active=@Active
                            WHERE Id=@Id";
                        cmd.Parameters.AddWithValue("@Id", device.Id);
                    }

                    cmd.Parameters.AddWithValue("@Name", device.Name);
                    cmd.Parameters.AddWithValue("@SerialPort", (int)device.SerialPort);
                    cmd.Parameters.AddWithValue("@BaudRate", (int)device.BaudRate);
                    cmd.Parameters.AddWithValue("@Parity", (int)device.Parity);
                    cmd.Parameters.AddWithValue("@SlaveId", device.SlaveId);
                    cmd.Parameters.AddWithValue("@Active", device.Active ? 1 : 0);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a Modbus RTU device from the database.
        /// </summary>
        private void DeleteDevice(ModbusRTUDevice device)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ModbusRTUDevice WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", device.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Customizes DataGrid column headers and controls for specific enum types.
        /// </summary>
        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var prop = e.PropertyName;

            switch (prop)
            {
                case nameof(ModbusRTUDevice.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;

                case nameof(ModbusRTUDevice.Name):
                    e.Column.Header = "Name";
                    break;

                case nameof(ModbusRTUDevice.Active):
                    e.Column.Header = "Active";
                    break;

                case nameof(ModbusRTUDevice.SerialPort):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Serial Port",
                        ItemsSource = Enum.GetValues(typeof(SerialPortName)),
                        SelectedItemBinding = new Binding(nameof(ModbusRTUDevice.SerialPort))
                    };
                    break;

                case nameof(ModbusRTUDevice.BaudRate):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Baud Rate",
                        ItemsSource = Enum.GetValues(typeof(BaudRate)),
                        SelectedItemBinding = new Binding(nameof(ModbusRTUDevice.BaudRate))
                    };
                    break;

                case nameof(ModbusRTUDevice.Parity):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Parity",
                        ItemsSource = Enum.GetValues(typeof(SerialParity)),
                        SelectedItemBinding = new Binding(nameof(ModbusRTUDevice.Parity))
                    };
                    break;

                case nameof(ModbusRTUDevice.SlaveId):
                    e.Column.Header = "Slave ID";
                    break;

                default:
                    e.Column.Visibility = Visibility.Hidden;
                    break;
            }
        }

        /// <summary>
        /// Saves all changes to the devices list.
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var device in _devices)
                    SaveDevice(device);

                LoadDevices();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _devices;

                Growl.Success("Changes saved successfully.");
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a new Modbus RTU device with default values.
        /// </summary>
        private void plusActivity_Click(object sender, RoutedEventArgs e)
        {
            var newDevice = new ModbusRTUDevice
            {
                Name = "New Device",
                SerialPort = SerialPortName.COM1,
                BaudRate = BaudRate.Baud9600,
                Parity = SerialParity.None,
                Active = true,
                SlaveId = 1
            };

            SaveDevice(newDevice);
            LoadDevices();
            userGrid.ItemsSource = null;
            userGrid.ItemsSource = _devices;

            Growl.Success("Device added.");
        }

        /// <summary>
        /// Removes the selected Modbus RTU device from the database.
        /// </summary>
        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            if (userGrid.SelectedItem is ModbusRTUDevice selected)
            {
                DeleteDevice(selected);
                LoadDevices();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _devices;

                Growl.Success("Device removed.");
            }
            else
            {
                Growl.Warning("Please select a device to remove.");
            }
        }
    }
}
