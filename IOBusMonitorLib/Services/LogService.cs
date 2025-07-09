using System;
using System.IO;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Simple file-based logger that writes one UTF-8 text file per day
    /// into the <c>Logs</c> folder inside the application directory.
    /// </summary>
    public static class LogService
    {
        private static readonly object _lock = new object();

        /// <summary>Writes an informational entry to today’s log.</summary>
        public static void LogInfo(string message) => WriteLog("INFO", message);

        /// <summary>Writes an error entry to today’s log.</summary>
        public static void LogError(string message) => WriteLog("ERROR", message);

        // --------------------------------------------------------------------
        // Internal helper – formatted append with thread-safety
        // --------------------------------------------------------------------
        private static void WriteLog(string level, string message)
        {
            try
            {
                string folder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Logs");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string file = Path.Combine(
                    folder,
                    "log_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

                string line = DateTime.Now.ToString("HH:mm:ss") +
                              " [" + level + "] " + message + Environment.NewLine;

                lock (_lock)
                {
                    File.AppendAllText(file, line);
                }
            }
            catch
            {
                // Logging failed – swallow to avoid recursive errors.
            }
        }
    }
}
