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
    /// Admin page for managing Modbus RTU measurement points.
    /// Allows adding, editing and deleting point entries associated with devices.
    /// </summary>
    public partial class ModbusRTUPointAdminPage : Page
    {
        private List<ModbusRTUPoint> _points;
        private List<ModbusRTUDevice> _devices;
        private readonly string _dbFile;

        public ModbusRTUPointAdminPage()
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
        /// Loads available Modbus RTU devices from the database for reference.
        /// </summary>
        private void LoadDevices()
        {
            _devices = new List<ModbusRTUDevice>();

            if (!File.Exists(_dbFile))
                return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Name FROM ModbusRTUDevice";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _devices.Add(new ModbusRTUDevice
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Loads Modbus RTU points from the database.
        /// </summary>
        private void LoadPoints()
        {
            _points = new List<ModbusRTUPoint>();

            if (!File.Exists(_dbFile))
                return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM ModbusRTUPoint";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _points.Add(new ModbusRTUPoint
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader["Name"].ToString(),
                            ModbusRTUDeviceId = reader.GetInt32(reader.GetOrdinal("ModbusRTUDeviceId"))
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Inserts or updates a point in the database.
        /// </summary>
        private void SavePoint(ModbusRTUPoint point)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                var cmd = connection.CreateCommand();

                if (point.Id == 0)
                {
                    cmd.CommandText = @"
                        INSERT INTO ModbusRTUPoint (Name, ModbusRTUDeviceId)
                        VALUES (@Name, @DeviceId)";
                }
                else
                {
                    cmd.CommandText = @"
                        UPDATE ModbusRTUPoint
                        SET Name=@Name, ModbusRTUDeviceId=@DeviceId
                        WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", point.Id);
                }

                cmd.Parameters.AddWithValue("@Name", point.Name);
                cmd.Parameters.AddWithValue("@DeviceId", point.ModbusRTUDeviceId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Deletes a point from the database.
        /// </summary>
        private void DeletePoint(ModbusRTUPoint point)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM ModbusRTUPoint WHERE Id=@Id";
                deleteCmd.Parameters.AddWithValue("@Id", point.Id);
                deleteCmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Dynamically generates columns for the DataGrid and binds enum/device fields as combo boxes.
        /// </summary>
        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ModbusRTUPoint.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;

                case nameof(ModbusRTUPoint.Name):
                    e.Column.Header = "Point Name";
                    break;

                case nameof(ModbusRTUPoint.ModbusRTUDeviceId):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Device",
                        ItemsSource = _devices,
                        SelectedValueBinding = new Binding(nameof(ModbusRTUPoint.ModbusRTUDeviceId)),
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
        /// Saves all changes made to the points list.
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
        /// Adds a new point with default values.
        /// </summary>
        private void plusActivity_Click(object sender, RoutedEventArgs e)
        {
            var newPoint = new ModbusRTUPoint
            {
                Name = "New Point",
                ModbusRTUDeviceId = _devices.FirstOrDefault()?.Id ?? 0
            };

            SavePoint(newPoint);
            LoadPoints();
            userGrid.ItemsSource = null;
            userGrid.ItemsSource = _points;
            Growl.Success("Point created.");
        }

        /// <summary>
        /// Deletes the currently selected point from the grid.
        /// </summary>
        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            if (userGrid.SelectedItem is ModbusRTUPoint selected)
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
