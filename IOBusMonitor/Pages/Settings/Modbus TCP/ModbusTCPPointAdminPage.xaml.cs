using HandyControl.Controls;
using IOBusMonitorLib;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace IOBusMonitor
{
    /// <summary>
    /// Admin page for managing Modbus TCP/IP measurement points.
    /// Allows binding each point to a Modbus TCP device.
    /// </summary>
    public partial class ModbusTCPPointAdminPage : Page
    {
        private List<ModbusTCPPoint> _points;
        private List<ModbusTCPDevice> _devices;
        private readonly string _dbFile;

        public ModbusTCPPointAdminPage()
        {
            InitializeComponent();
            _dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "Settings.db");

            try
            {
                LoadDevices();
                LoadPoints();
                userGrid.ItemsSource = _points;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads all Modbus TCP devices for reference binding.
        /// </summary>
        private void LoadDevices()
        {
            _devices = new List<ModbusTCPDevice>();

            if (!File.Exists(_dbFile)) return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Name FROM ModbusTCPDevice";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _devices.Add(new ModbusTCPDevice
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Loads all measurement points from the database.
        /// </summary>
        private void LoadPoints()
        {
            _points = new List<ModbusTCPPoint>();

            if (!File.Exists(_dbFile)) return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM ModbusTCPPoint";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _points.Add(new ModbusTCPPoint
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader["Name"].ToString(),
                            ModbusTCPDeviceId = reader.GetInt32(reader.GetOrdinal("ModbusTCPDeviceId"))
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Saves a single point to the database (insert or update).
        /// </summary>
        private void SavePoint(ModbusTCPPoint point)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                var cmd = connection.CreateCommand();

                if (point.Id == 0)
                {
                    cmd.CommandText = @"
                        INSERT INTO ModbusTCPPoint (Name, ModbusTCPDeviceId)
                        VALUES (@Name, @DeviceId)";
                }
                else
                {
                    cmd.CommandText = @"
                        UPDATE ModbusTCPPoint
                        SET Name=@Name, ModbusTCPDeviceId=@DeviceId
                        WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", point.Id);
                }

                cmd.Parameters.AddWithValue("@Name", point.Name);
                cmd.Parameters.AddWithValue("@DeviceId", point.ModbusTCPDeviceId);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Deletes a point from the database.
        /// </summary>
        private void DeletePoint(ModbusTCPPoint point)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM ModbusTCPPoint WHERE Id=@Id";
                cmd.Parameters.AddWithValue("@Id", point.Id);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Configures DataGrid columns for point editing and device binding.
        /// </summary>
        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ModbusTCPPoint.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;

                case nameof(ModbusTCPPoint.Name):
                    e.Column.Header = "Point Name";
                    break;

                case nameof(ModbusTCPPoint.ModbusTCPDeviceId):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Device",
                        ItemsSource = _devices,
                        SelectedValueBinding = new Binding(nameof(ModbusTCPPoint.ModbusTCPDeviceId)),
                        DisplayMemberPath = "Name",
                        SelectedValuePath = "Id"
                    };
                    break;

                default:
                    e.Column.Visibility = Visibility.Hidden;
                    break;
            }
        }

        /// <summary>
        /// Saves all points in the current list.
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var p in _points)
                    SavePoint(p);

                LoadPoints();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _points;
                Growl.Success("Changes saved successfully.");
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a new measurement point with default values.
        /// </summary>
        private void plusActivity_Click(object sender, RoutedEventArgs e)
        {
            var newPoint = new ModbusTCPPoint
            {
                Name = "New Point",
                ModbusTCPDeviceId = _devices.FirstOrDefault()?.Id ?? 0
            };

            SavePoint(newPoint);
            LoadPoints();
            userGrid.ItemsSource = null;
            userGrid.ItemsSource = _points;
            Growl.Success("Point created.");
        }

        /// <summary>
        /// Deletes the selected point.
        /// </summary>
        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            if (userGrid.SelectedItem is ModbusTCPPoint selected)
            {
                DeletePoint(selected);
                LoadPoints();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _points;
                Growl.Success("Point deleted.");
            }
            else
            {
                Growl.Warning("Please select a point to delete.");
            }
        }
    }
}
