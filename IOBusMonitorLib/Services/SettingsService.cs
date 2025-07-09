using System;
using System.Data.SQLite;
using System.IO;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Persists application-wide settings in <c>Settings/Settings.db</c>.
    /// Creates the file and table automatically if they do not exist.
    /// </summary>
    public class SettingsService
    {
        // ---------- helpers --------------------------------------------------

        /// <summary>Returns the full path to Settings.db, creating the folder if needed.</summary>
        private string GetDbPath()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, "Settings.db");
        }

        /// <summary>
        /// Ensures the SQLite file and <c>AppSettings</c> table exist.
        /// Inserts one default row if the database was newly created.
        /// </summary>
        private void EnsureDatabaseAndTableExist(string dbFile)
        {
            bool newDb = !File.Exists(dbFile);

            using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
            {
                conn.Open();

                if (newDb)
                {
                    var create = conn.CreateCommand();
                    create.CommandText =
        @"CREATE TABLE AppSettings (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              ReadIntervalMs INTEGER,
              AutoStart INTEGER,
              PathData TEXT
          )";
                    create.ExecuteNonQuery();

                    var insert = conn.CreateCommand();
                    insert.CommandText =
        @"INSERT INTO AppSettings (ReadIntervalMs, AutoStart, PathData)
          VALUES (1000, 0, @Path)";
                    insert.Parameters.AddWithValue("@Path",
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
                    insert.ExecuteNonQuery();
                }
            }
        }

        // ---------- public API ----------------------------------------------

        /// <summary>Loads the single <see cref="AppSettings"/> record.</summary>
        public AppSettings LoadSettings()
        {
            string dbFile = GetDbPath();
            EnsureDatabaseAndTableExist(dbFile);

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM AppSettings LIMIT 1";
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                return new AppSettings
                                {
                                    ReadIntervalMs = r.GetInt32(r.GetOrdinal("ReadIntervalMs")),
                                    AutoStart = r.GetInt32(r.GetOrdinal("AutoStart")) == 1,
                                    PathData = r["PathData"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Settings DB error: " + ex.Message);
            }

            // Fallback defaults
            return new AppSettings
            {
                ReadIntervalMs = 1000,
                AutoStart = false,
                PathData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
            };
        }

        /// <summary>Saves (or inserts) the single <see cref="AppSettings"/> row.</summary>
        public void SaveSettings(AppSettings settings)
        {
            string dbFile = GetDbPath();
            EnsureDatabaseAndTableExist(dbFile);

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbFile + ";"))
                {
                    conn.Open();

                    // Determine if a row already exists
                    var check = conn.CreateCommand();
                    check.CommandText = "SELECT COUNT(*) FROM AppSettings";
                    long count = (long)check.ExecuteScalar();

                    SQLiteCommand cmd;
                    if (count > 0)
                    {
                        cmd = conn.CreateCommand();
                        cmd.CommandText =
        @"UPDATE AppSettings
          SET ReadIntervalMs=@Interval, AutoStart=@Auto, PathData=@Path";
                    }
                    else
                    {
                        cmd = conn.CreateCommand();
                        cmd.CommandText =
        @"INSERT INTO AppSettings (ReadIntervalMs, AutoStart, PathData)
          VALUES (@Interval, @Auto, @Path)";
                    }

                    cmd.Parameters.AddWithValue("@Interval", settings.ReadIntervalMs);
                    cmd.Parameters.AddWithValue("@Auto", settings.AutoStart ? 1 : 0);
                    cmd.Parameters.AddWithValue("@Path", settings.PathData);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to save settings: " + ex.Message);
            }
        }
    }
}
