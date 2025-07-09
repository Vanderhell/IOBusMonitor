using System;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Custom exception thrown when a connection to a PLC cannot be
    /// established or is lost unexpectedly.
    /// </summary>
    public class PlcConnectionException : Exception
    {
        /// <summary>
        /// Name of the device that caused the connection failure.
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        /// Creates a new instance of <see cref="PlcConnectionException"/>.
        /// </summary>
        /// <param name="deviceName">Human-readable device name.</param>
        /// <param name="message">Detailed error message.</param>
        /// <param name="innerException">
        /// Optional inner exception that triggered this failure.
        /// </param>
        public PlcConnectionException(
            string deviceName,
            string message,
            Exception innerException = null)
            : base(message, innerException)
        {
            DeviceName = deviceName;
        }
    }
}
