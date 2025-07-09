namespace IOBusMonitorLib
{
    /// <summary>
    /// Parity options for serial (Modbus-RTU) communication.
    /// Values map 1-to-1 to <see cref="System.IO.Ports.Parity"/>.
    /// </summary>
    public enum SerialParity : int
    {
        /// <summary>No parity bit.</summary>
        None = 0,

        /// <summary>Even parity.</summary>
        Even = 1,

        /// <summary>Odd parity.</summary>
        Odd = 2,

        /// <summary>Mark parity (parity bit is always 1).</summary>
        Mark = 3,

        /// <summary>Space parity (parity bit is always 0).</summary>
        Space = 4
    }
}
