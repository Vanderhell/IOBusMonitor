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
    /// Admin page for managing Modbus RTU measurements.
    /// Loads and saves data from SQLite database and binds it to a DataGrid.
    /// </summary>
    public partial class ModbusRTUMeasurementAdminPage : Page
    {
        private List<RTUMeasurement> _measurements;
        private List<ModbusRTUPoint> _points;
        private readonly string _dbFile;

        public ModbusRTUMeasurementAdminPage()
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

        /// <summary>
        /// Loads available Modbus RTU points for measurement assignment.
        /// </summary>
        private void LoadPoints()
        {
            _points = new List<ModbusRTUPoint>();

            if (!File.Exists(_dbFile)) return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Name FROM ModbusRTUPoint";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _points.Add(new ModbusRTUPoint
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader["Name"].ToString()
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads all RTU measurements from the database.
        /// </summary>
        private void LoadMeasurements()
        {
            _measurements = new List<RTUMeasurement>();

            if (!File.Exists(_dbFile)) return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM RTUMeasurement";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _measurements.Add(new RTUMeasurement
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader["Name"].ToString(),
                                Unit = reader["Unit"].ToString(),
                                Round = reader.GetInt32(reader.GetOrdinal("Round")),
                                Condition = reader["Condition"].ToString(),
                                Register = reader.GetInt32(reader.GetOrdinal("Register")),
                                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                                Active = reader.GetInt32(reader.GetOrdinal("Active")) == 1,
                                BitOrder = (BitOrder)Enum.Parse(typeof(BitOrder), reader["BitOrder"].ToString()),
                                ModbusRTUPointId = reader.GetInt32(reader.GetOrdinal("ModbusRTUPointId"))
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves a measurement to the database (insert or update).
        /// </summary>
        private void SaveMeasurement(RTUMeasurement m)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    if (m.Id == 0)
                    {
                        cmd.CommandText = @"
                            INSERT INTO RTUMeasurement (Name, Unit, Round, Condition, Register, Quantity, Active, BitOrder, ModbusRTUPointId)
                            VALUES (@Name, @Unit, @Round, @Condition, @Register, @Quantity, @Active, @BitOrder, @ModbusRTUPointId)";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE RTUMeasurement
                            SET Name=@Name, Unit=@Unit, Round=@Round, Condition=@Condition,
                                Register=@Register, Quantity=@Quantity, Active=@Active,
                                BitOrder=@BitOrder, ModbusRTUPointId=@ModbusRTUPointId
                            WHERE Id=@Id";
                        cmd.Parameters.AddWithValue("@Id", m.Id);
                    }

                    cmd.Parameters.AddWithValue("@Name", m.Name);
                    cmd.Parameters.AddWithValue("@Unit", m.Unit);
                    cmd.Parameters.AddWithValue("@Round", m.Round);
                    cmd.Parameters.AddWithValue("@Condition", m.Condition);
                    cmd.Parameters.AddWithValue("@Register", m.Register);
                    cmd.Parameters.AddWithValue("@Quantity", m.Quantity);
                    cmd.Parameters.AddWithValue("@Active", m.Active ? 1 : 0);
                    cmd.Parameters.AddWithValue("@BitOrder", (int)m.BitOrder);
                    cmd.Parameters.AddWithValue("@ModbusRTUPointId", m.ModbusRTUPointId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a measurement from the database.
        /// </summary>
        private void DeleteMeasurement(RTUMeasurement m)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM RTUMeasurement WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", m.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Configures how each property is displayed in the DataGrid.
        /// </summary>
        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(RTUMeasurement.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;

                case nameof(RTUMeasurement.Name):
                    e.Column.Header = "Measurement Name";
                    break;

                case nameof(RTUMeasurement.Unit):
                    e.Column.Header = "Unit";
                    break;

                case nameof(RTUMeasurement.Round):
                    e.Column.Header = "Rounding";
                    break;

                case nameof(RTUMeasurement.Condition):
                    e.Column.Header = "Condition";
                    break;

                case nameof(RTUMeasurement.Register):
                    e.Column.Header = "Register";
                    break;

                case nameof(RTUMeasurement.Quantity):
                    e.Column.Header = "Quantity";
                    break;

                case nameof(RTUMeasurement.Active):
                    e.Column.Header = "Active";
                    break;

                case nameof(RTUMeasurement.BitOrder):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Bit Order",
                        ItemsSource = Enum.GetValues(typeof(BitOrder)),
                        SelectedItemBinding = new Binding(nameof(RTUMeasurement.BitOrder))
                    };
                    break;

                case nameof(RTUMeasurement.ModbusRTUPointId):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Measurement Point",
                        ItemsSource = _points,
                        SelectedValueBinding = new Binding(nameof(RTUMeasurement.ModbusRTUPointId)),
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
        /// Saves all edited measurements.
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var m in _measurements)
                    SaveMeasurement(m);

                LoadMeasurements();
                userGrid.ItemsSource = null;
                userGrid.ItemsSource = _measurements;
                Growl.Success("Changes saved successfully.");
            }
            catch (Exception ex)
            {
                Growl.Error($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a new measurement with default values.
        /// </summary>
        private void plusActivity_Click(object sender, RoutedEventArgs e)
        {
            var newMeasurement = new RTUMeasurement
            {
                Name = "New Measurement",
                Unit = "Unit",
                Round = 2,
                Condition = "value",
                Register = 0,
                Quantity = 1,
                Active = true,
                BitOrder = BitOrder.Normal,
                ModbusRTUPointId = _points.FirstOrDefault()?.Id ?? 0
            };

            SaveMeasurement(newMeasurement);
            LoadMeasurements();
            userGrid.ItemsSource = null;
            userGrid.ItemsSource = _measurements;
            Growl.Success("Measurement created.");
        }

        /// <summary>
        /// Removes the selected measurement.
        /// </summary>
        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            if (userGrid.SelectedItem is RTUMeasurement selected)
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
