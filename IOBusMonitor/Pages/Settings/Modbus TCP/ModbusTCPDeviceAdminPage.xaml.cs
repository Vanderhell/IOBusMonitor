using HandyControl.Controls;
using IOBusMonitorLib;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace IOBusMonitor
{
    /// <summary>
    /// Admin page for managing Modbus TCP/IP devices.
    /// Supports loading, editing, adding and deleting devices from a SQLite database.
    /// </summary>
    public partial class ModbusTCPDeviceAdminPage : Page
    {
        private List<ModbusTCPDevice> _devices;
        private readonly string _dbFile;

        public ModbusTCPDeviceAdminPage()
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
        /// Loads all Modbus TCP/IP devices from the SQLite database.
        /// </summary>
        private void LoadDevices()
        {
            _devices = new List<ModbusTCPDevice>();

            if (!File.Exists(_dbFile))
                return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ModbusTCPDevice";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _devices.Add(new ModbusTCPDevice
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader["Name"].ToString(),
                                IPAddress = reader["IPAddress"].ToString(),
                                Port = reader.GetInt32(reader.GetOrdinal("Port")),
                                Active = reader.GetInt32(reader.GetOrdinal("Active")) == 1
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves a device to the database (insert or update).
        /// </summary>
        private void SaveDevice(ModbusTCPDevice device)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    if (device.Id == 0)
                    {
                        cmd.CommandText = @"
                            INSERT INTO ModbusTCPDevice (Name, IPAddress, Port, Active)
                            VALUES (@Name, @IPAddress, @Port, @Active)";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE ModbusTCPDevice
                            SET Name=@Name, IPAddress=@IPAddress, Port=@Port, Active=@Active
                            WHERE Id=@Id";
                        cmd.Parameters.AddWithValue("@Id", device.Id);
                    }

                    cmd.Parameters.AddWithValue("@Name", device.Name);
                    cmd.Parameters.AddWithValue("@IPAddress", device.IPAddress);
                    cmd.Parameters.AddWithValue("@Port", device.Port);
                    cmd.Parameters.AddWithValue("@Active", device.Active ? 1 : 0);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a device from the database.
        /// </summary>
        private void DeleteDevice(ModbusTCPDevice device)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var deleteCmd = connection.CreateCommand())
                {
                    deleteCmd.CommandText = "DELETE FROM ModbusTCPDevice WHERE Id=@Id";
                    deleteCmd.Parameters.AddWithValue("@Id", device.Id);
                    deleteCmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Customizes the DataGrid column headers and behavior.
        /// </summary>
        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ModbusTCPDevice.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;

                case nameof(ModbusTCPDevice.Name):
                    e.Column.Header = "Device Name";
                    break;

                case nameof(ModbusTCPDevice.IPAddress):
                    e.Column.Header = "IP Address";
                    break;

                case nameof(ModbusTCPDevice.Port):
                    e.Column.Header = "Port";
                    break;

                case nameof(ModbusTCPDevice.Active):
                    e.Column.Header = "Active";
                    break;

                default:
                    e.Column.Visibility = Visibility.Hidden;
                    break;
            }
        }

        /// <summary>
        /// Saves all edited devices in the list.
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var d in _devices)
                    SaveDevice(d);

                LoadDevices();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _devices;
                Growl.Success("Changes saved.");
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new device with default values.
        /// </summary>
        private void plusActivity_Click(object sender, RoutedEventArgs e)
        {
            var newDevice = new ModbusTCPDevice
            {
                Name = "New Device",
                IPAddress = "127.0.0.1",
                Port = 502,
                Active = true
            };

            SaveDevice(newDevice);
            LoadDevices();
            userGrid.ItemsSource = null;
            userGrid.ItemsSource = _devices;
            Growl.Success("Device created.");
        }

        /// <summary>
        /// Removes the selected device from the list and database.
        /// </summary>
        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            if (userGrid.SelectedItem is ModbusTCPDevice selected)
            {
                DeleteDevice(selected);
                LoadDevices();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _devices;
                Growl.Success("Device deleted.");
            }
            else
            {
                Growl.Warning("Please select a device to delete.");
            }
        }
    }
}
