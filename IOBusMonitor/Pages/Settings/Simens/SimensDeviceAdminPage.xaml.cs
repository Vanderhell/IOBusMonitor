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
    /// Admin page for managing Siemens S7 devices.
    /// Provides CRUD functionality for device records stored in SQLite.
    /// </summary>
    public partial class SimensDeviceAdminPage : Page
    {
        private List<SimensDevice> _devices;
        private readonly string _dbFile;

        public SimensDeviceAdminPage()
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

        /// <summary>Loads all Siemens devices from the database.</summary>
        private void LoadDevices()
        {
            _devices = new List<SimensDevice>();

            if (!File.Exists(_dbFile)) return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM SimensDevice";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _devices.Add(new SimensDevice
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader["Name"].ToString(),
                                IPAddress = reader["IPAddress"].ToString(),
                                Port = reader.GetInt32(reader.GetOrdinal("Port")),
                                Rack = reader.GetInt32(reader.GetOrdinal("Rack")),
                                Slot = reader.GetInt32(reader.GetOrdinal("Slot")),
                                CpuType = (CpuType)Enum.Parse(typeof(CpuType), reader["CpuType"].ToString()),
                                Active = reader.GetInt32(reader.GetOrdinal("Active")) == 1
                            });
                        }
                    }
                }
            }
        }

        /// <summary>Inserts or updates a Siemens device.</summary>
        private void SaveDevice(SimensDevice device)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    if (device.Id == 0)
                    {
                        cmd.CommandText = @"
                            INSERT INTO SimensDevice (Name, IPAddress, Port, Rack, Slot, CpuType, Active)
                            VALUES (@Name, @IPAddress, @Port, @Rack, @Slot, @CpuType, @Active)";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE SimensDevice
                            SET Name=@Name, IPAddress=@IPAddress, Port=@Port, Rack=@Rack,
                                Slot=@Slot, CpuType=@CpuType, Active=@Active
                            WHERE Id=@Id";
                        cmd.Parameters.AddWithValue("@Id", device.Id);
                    }

                    cmd.Parameters.AddWithValue("@Name", device.Name);
                    cmd.Parameters.AddWithValue("@IPAddress", device.IPAddress);
                    cmd.Parameters.AddWithValue("@Port", device.Port);
                    cmd.Parameters.AddWithValue("@Rack", device.Rack);
                    cmd.Parameters.AddWithValue("@Slot", device.Slot);
                    cmd.Parameters.AddWithValue("@CpuType", (int)device.CpuType);
                    cmd.Parameters.AddWithValue("@Active", device.Active ? 1 : 0);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Deletes the specified device.</summary>
        private void DeleteDevice(SimensDevice device)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM SimensDevice WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", device.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Configures DataGrid columns (headers, combo boxes, visibility).</summary>
        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SimensDevice.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;

                case nameof(SimensDevice.Name):
                    e.Column.Header = "Device Name";
                    break;

                case nameof(SimensDevice.Active):
                    e.Column.Header = "Active";
                    break;

                case nameof(SimensDevice.IPAddress):
                    e.Column.Header = "IP Address";
                    break;

                case nameof(SimensDevice.Port):
                    e.Column.Header = "Port";
                    break;

                case nameof(SimensDevice.Rack):
                    e.Column.Header = "Rack";
                    break;

                case nameof(SimensDevice.Slot):
                    e.Column.Header = "Slot";
                    break;

                case nameof(SimensDevice.CpuType):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "CPU Type",
                        ItemsSource = Enum.GetValues(typeof(CpuType)),
                        SelectedItemBinding = new Binding(nameof(SimensDevice.CpuType))
                    };
                    break;

                default:
                    e.Column.Visibility = Visibility.Hidden;
                    break;
            }
        }

        // ---------- Button Handlers ----------

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var d in _devices) SaveDevice(d);

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

        private void plusActivity_Click(object sender, RoutedEventArgs e)
        {
            var newDevice = new SimensDevice
            {
                Name = "New Device",
                IPAddress = "192.168.0.1",
                Port = 102,
                Rack = 0,
                Slot = 1,
                CpuType = CpuType.S71200,
                Active = true
            };

            SaveDevice(newDevice);
            LoadDevices();
            userGrid.ItemsSource = null;
            userGrid.ItemsSource = _devices;
            Growl.Success("Device created.");
        }

        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            if (userGrid.SelectedItem is SimensDevice selected)
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
