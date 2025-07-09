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
    /// Admin page for Siemens S7 measurement points (C# 7.3 compatible).
    /// </summary>
    public partial class SimensPointAdminPage : Page
    {
        private List<SimensPoint> _points;
        private List<SimensDevice> _devices;
        private readonly string _dbFile;

        public SimensPointAdminPage()
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

        // ---------- Load helpers ----------

        private void LoadDevices()
        {
            _devices = new List<SimensDevice>();
            if (!File.Exists(_dbFile)) return;

            using (var conn = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Name FROM SimensDevice";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            _devices.Add(new SimensDevice
                            {
                                Id = r.GetInt32(0),
                                Name = r["Name"].ToString()
                            });
                        }
                    }
                }
            }
        }

        private void LoadPoints()
        {
            _points = new List<SimensPoint>();
            if (!File.Exists(_dbFile)) return;

            using (var conn = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM SimensPoint";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            _points.Add(new SimensPoint
                            {
                                Id = r.GetInt32(r.GetOrdinal("Id")),
                                Name = r["Name"].ToString(),
                                SimenseDeviceId = r.GetInt32(r.GetOrdinal("SimenseDeviceId"))
                            });
                        }
                    }
                }
            }
        }

        // ---------- CRUD ----------

        private void SavePoint(SimensPoint p)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    if (p.Id == 0)
                    {
                        cmd.CommandText = "INSERT INTO SimensPoint (Name, SimenseDeviceId) VALUES (@Name, @DeviceId)";
                    }
                    else
                    {
                        cmd.CommandText = "UPDATE SimensPoint SET Name=@Name, SimenseDeviceId=@DeviceId WHERE Id=@Id";
                        cmd.Parameters.AddWithValue("@Id", p.Id);
                    }

                    cmd.Parameters.AddWithValue("@Name", p.Name);
                    cmd.Parameters.AddWithValue("@DeviceId", p.SimenseDeviceId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void DeletePoint(SimensPoint p)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM SimensPoint WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", p.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ---------- DataGrid config ----------

        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SimensPoint.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;

                case nameof(SimensPoint.Name):
                    e.Column.Header = "Point Name";
                    break;

                case nameof(SimensPoint.SimenseDeviceId):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Device",
                        ItemsSource = _devices,
                        SelectedValueBinding = new Binding(nameof(SimensPoint.SimenseDeviceId)),
                        DisplayMemberPath = "Name",
                        SelectedValuePath = "Id"
                    };
                    break;

                default:
                    e.Column.Visibility = Visibility.Hidden;
                    break;
            }
        }

        // ---------- Buttons ----------

        private void plusActivity_Click(object sender, RoutedEventArgs e)
        {
            var p = new SimensPoint
            {
                Name = "New Point",
                SimenseDeviceId = _devices.FirstOrDefault()?.Id ?? 0
            };
            SavePoint(p);
            LoadPoints();
            userGrid.ItemsSource = _points;
            Growl.Success("Point created.");
        }

        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            var sel = userGrid.SelectedItem as SimensPoint;
            if (sel == null)
            {
                Growl.Warning("Select a point to delete.");
                return;
            }

            DeletePoint(sel);
            LoadPoints();
            userGrid.ItemsSource = _points;
            Growl.Success("Point deleted.");
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var p in _points) SavePoint(p);
                LoadPoints();
                userGrid.ItemsSource = _points;
                Growl.Success("Changes saved.");
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }
    }
}
