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
    /// Admin page for Siemens S7 measurements.
    /// Provides CRUD operations for measurement definitions stored in SQLite.
    /// </summary>
    public partial class SimensMeasurementAdminPage : Page
    {
        private List<SimensMeasurement> _measurements;
        private List<SimensPoint> _points;
        private readonly string _dbFile;

        public SimensMeasurementAdminPage()
        {
            InitializeComponent();
            _dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "Settings.db");

            try
            {
                LoadPoints();
                LoadMeasurements();
                userGrid.ItemsSource = _measurements;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }

        // ---------- Data Loading ----------

        private void LoadPoints()
        {
            _points = new List<SimensPoint>();
            if (!File.Exists(_dbFile)) return;

            using (var conn = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Name FROM SimensPoint";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            _points.Add(new SimensPoint
                            {
                                Id = r.GetInt32(r.GetOrdinal("Id")),
                                Name = r["Name"].ToString()
                            });
                        }
                    }
                }
            }
        }

        private void LoadMeasurements()
        {
            _measurements = new List<SimensMeasurement>();
            if (!File.Exists(_dbFile)) return;

            using (var conn = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM SimensMeasurement";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            _measurements.Add(new SimensMeasurement
                            {
                                Id = r.GetInt32(r.GetOrdinal("Id")),
                                Name = r["Name"].ToString(),
                                Unit = r["Unit"].ToString(),
                                Round = r.GetInt32(r.GetOrdinal("Round")),
                                Condition = r["Condition"].ToString(),
                                Address = r["Address"].ToString(),
                                SimensPointId = r.GetInt32(r.GetOrdinal("SimensPointId")),
                                Active = r.GetInt32(r.GetOrdinal("Active")) == 1
                            });
                        }
                    }
                }
            }
        }

        // ---------- CRUD ----------

        private void SaveMeasurement(SimensMeasurement m)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    if (m.Id == 0)
                    {
                        cmd.CommandText = @"
                            INSERT INTO SimensMeasurement
                                (Name, Unit, Round, Condition, Address, SimensPointId, Active)
                            VALUES
                                (@Name, @Unit, @Round, @Condition, @Address, @SimensPointId, @Active)";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE SimensMeasurement
                            SET Name=@Name, Unit=@Unit, Round=@Round, Condition=@Condition,
                                Address=@Address, SimensPointId=@SimensPointId, Active=@Active
                            WHERE Id=@Id";
                        cmd.Parameters.AddWithValue("@Id", m.Id);
                    }

                    cmd.Parameters.AddWithValue("@Name", m.Name);
                    cmd.Parameters.AddWithValue("@Unit", m.Unit);
                    cmd.Parameters.AddWithValue("@Round", m.Round);
                    cmd.Parameters.AddWithValue("@Condition", m.Condition);
                    cmd.Parameters.AddWithValue("@Address", m.Address);
                    cmd.Parameters.AddWithValue("@SimensPointId", m.SimensPointId);
                    cmd.Parameters.AddWithValue("@Active", m.Active ? 1 : 0);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void DeleteMeasurement(SimensMeasurement m)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM SimensMeasurement WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", m.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ---------- DataGrid Config ----------

        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SimensMeasurement.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;
                case nameof(SimensMeasurement.Name):
                    e.Column.Header = "Measurement Name";
                    break;
                case nameof(SimensMeasurement.Active):
                    e.Column.Header = "Active";
                    break;
                case nameof(SimensMeasurement.Unit):
                    e.Column.Header = "Unit";
                    break;
                case nameof(SimensMeasurement.Round):
                    e.Column.Header = "Rounding";
                    break;
                case nameof(SimensMeasurement.Condition):
                    e.Column.Header = "Condition";
                    break;
                case nameof(SimensMeasurement.Address):
                    e.Column.Header = "Address";
                    break;
                case nameof(SimensMeasurement.SimensPointId):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Measurement Point",
                        ItemsSource = _points,
                        SelectedValueBinding = new Binding(nameof(SimensMeasurement.SimensPointId)),
                        DisplayMemberPath = "Name",
                        SelectedValuePath = "Id"
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
                foreach (var m in _measurements) SaveMeasurement(m);

                LoadMeasurements();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _measurements;
                Growl.Success("Changes saved.");
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }

        private void plusActivity_Click(object sender, RoutedEventArgs e)
        {
            var newMeasurement = new SimensMeasurement
            {
                Name = "New Measurement",
                Unit = "Unit",
                Round = 2,
                Condition = "value",
                Address = "DB1.DBD0",
                SimensPointId = _points.FirstOrDefault()?.Id ?? 0,
                Active = true
            };

            SaveMeasurement(newMeasurement);
            LoadMeasurements();
            userGrid.ItemsSource = null;
            userGrid.ItemsSource = _measurements;
            Growl.Success("Measurement created.");
        }

        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            if (userGrid.SelectedItem is SimensMeasurement selected)
            {
                DeleteMeasurement(selected);
                LoadMeasurements();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _measurements;
                Growl.Success("Measurement deleted.");
            }
            else
            {
                Growl.Warning("Please select a measurement to delete.");
            }
        }
    }
}
