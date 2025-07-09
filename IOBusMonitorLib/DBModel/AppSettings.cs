using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Application-wide settings persisted in the <c>AppSettings</c> table
    /// of <c>Settings.db</c>. One row (Id = 1) is used as a key-value store.
    /// </summary>
    [Table("AppSettings")]
    public class AppSettings
    {
        /// <summary>Primary-key value; always <c>1</c>.</summary>
        [Key]
        public int Id { get; set; } = 1;

        /// <summary>Polling interval for the timer service, in milliseconds.</summary>
        public int ReadIntervalMs { get; set; } = 1000;

        /// <summary>
        /// When <c>true</c>, the application starts monitoring immediately
        /// after launch.
        /// </summary>
        public bool AutoStart { get; set; } = false;

        /// <summary>
        /// Filesystem folder where daily measurement databases are written.
        /// If <c>null</c> or empty, the default <c>./Data</c> folder is used.
        /// </summary>
        public string PathData { get; set; }
    }
}
