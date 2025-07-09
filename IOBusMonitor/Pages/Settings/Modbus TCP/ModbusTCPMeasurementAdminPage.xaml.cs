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
    /// Admin page for managing Modbus TCP/IP measurement definitions.
    /// Allows adding, editing, and deleting TCP measurements stored in SQLite.
    /// </summary>
    public partial class ModbusTCPMeasurementAdminPage : Page
    {
        private List<TCPMeasurement> _measurements;
        private List<ModbusTCPPoint> _points;
        private readonly string _dbFile;

        public ModbusTCPMeasurementAdminPage()
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
        /// Loads all TCP points for assigning to measurements.
        /// </summary>
        private void LoadPoints()
        {
            _points = new List<ModbusTCPPoint>();

            if (!File.Exists(_dbFile))
                return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Name FROM ModbusTCPPoint";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _points.Add(new ModbusTCPPoint
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
        /// Loads all TCP measurement entries from the database.
        /// </summary>
        private void LoadMeasurements()
        {
            _measurements = new List<TCPMeasurement>();

            if (!File.Exists(_dbFile))
                return;

            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM TCPMeasurement";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _measurements.Add(new TCPMeasurement
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
                                ModbusTCPPointId = reader.GetInt32(reader.GetOrdinal("ModbusTCPPointId"))
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves a measurement to the database (insert or update).
        /// </summary>
        private void SaveMeasurement(TCPMeasurement measurement)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    if (measurement.Id == 0)
                    {
                        cmd.CommandText = @"
                            INSERT INTO TCPMeasurement (Name, Unit, Round, Condition, Register, Quantity, Active, BitOrder, ModbusTCPPointId)
                            VALUES (@Name, @Unit, @Round, @Condition, @Register, @Quantity, @Active, @BitOrder, @ModbusTCPPointId)";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE TCPMeasurement
                            SET Name=@Name, Unit=@Unit, Round=@Round, Condition=@Condition, Register=@Register,
                                Quantity=@Quantity, Active=@Active, BitOrder=@BitOrder, ModbusTCPPointId=@ModbusTCPPointId
                            WHERE Id=@Id";
                        cmd.Parameters.AddWithValue("@Id", measurement.Id);
                    }

                    cmd.Parameters.AddWithValue("@Name", measurement.Name);
                    cmd.Parameters.AddWithValue("@Unit", measurement.Unit);
                    cmd.Parameters.AddWithValue("@Round", measurement.Round);
                    cmd.Parameters.AddWithValue("@Condition", measurement.Condition);
                    cmd.Parameters.AddWithValue("@Register", measurement.Register);
                    cmd.Parameters.AddWithValue("@Quantity", measurement.Quantity);
                    cmd.Parameters.AddWithValue("@Active", measurement.Active ? 1 : 0);
                    cmd.Parameters.AddWithValue("@BitOrder", (int)measurement.BitOrder);
                    cmd.Parameters.AddWithValue("@ModbusTCPPointId", measurement.ModbusTCPPointId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes the selected measurement from the database.
        /// </summary>
        private void DeleteMeasurement(TCPMeasurement measurement)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbFile};"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM TCPMeasurement WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", measurement.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Configures columns in the DataGrid for editing measurements.
        /// </summary>
        private void userGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TCPMeasurement.Id):
                    e.Column.Header = "ID";
                    e.Column.IsReadOnly = true;
                    break;

                case nameof(TCPMeasurement.Name):
                    e.Column.Header = "Measurement Name";
                    break;

                case nameof(TCPMeasurement.Unit):
                    e.Column.Header = "Unit";
                    break;

                case nameof(TCPMeasurement.Round):
                    e.Column.Header = "Rounding";
                    break;

                case nameof(TCPMeasurement.Condition):
                    e.Column.Header = "Condition";
                    break;

                case nameof(TCPMeasurement.Register):
                    e.Column.Header = "Register";
                    break;

                case nameof(TCPMeasurement.Quantity):
                    e.Column.Header = "Quantity";
                    break;

                case nameof(TCPMeasurement.Active):
                    e.Column.Header = "Active";
                    break;

                case nameof(TCPMeasurement.BitOrder):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Bit Order",
                        ItemsSource = Enum.GetValues(typeof(BitOrder)),
                        SelectedItemBinding = new Binding(nameof(TCPMeasurement.BitOrder))
                    };
                    break;

                case nameof(TCPMeasurement.ModbusTCPPointId):
                    e.Column = new DataGridComboBoxColumn
                    {
                        Header = "Measurement Point",
                        ItemsSource = _points,
                        SelectedValueBinding = new Binding(nameof(TCPMeasurement.ModbusTCPPointId)),
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
        /// Saves all modified measurements.
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
            var newMeasurement = new TCPMeasurement
            {
                Name = "New Measurement",
                Unit = "Unit",
                Round = 2,
                Condition = "value",
                Register = 0,
                Quantity = 1,
                Active = true,
                BitOrder = BitOrder.Normal,
                ModbusTCPPointId = _points.FirstOrDefault()?.Id ?? 0
            };

            SaveMeasurement(newMeasurement);
            LoadMeasurements();
            userGrid.ItemsSource = null;
            userGrid.ItemsSource = _measurements;
            Growl.Success("Measurement created.");
        }

        /// <summary>
        /// Deletes the currently selected measurement.
        /// </summary>
        private void minusActivity_Click(object sender, RoutedEventArgs e)
        {
            if (userGrid.SelectedItem is TCPMeasurement selected)
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
