using System.IO.Ports;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Helper methods for mapping enum values defined in the project
    /// to <see cref="System.IO.Ports.SerialPort"/>-specific values.
    /// </summary>
    public static class SerialPortHelper
    {
        /// <summary>
        /// Returns the string name of a COM port (e.g.&nbsp;<c>"COM3"</c>)
        /// from the project’s <see cref="SerialPortName"/> enum.
        /// </summary>
        public static string GetSerialPortName(SerialPortName portName)
        {
            return portName.ToString();
        }

        /// <summary>
        /// Maps the project’s <see cref="SerialParity"/> enum to the
        /// <see cref="Parity"/> value required by <see cref="SerialPort"/>.
        /// </summary>
        public static Parity GetParity(SerialParity parity)
        {
            switch (parity)
            {
                case SerialParity.None: return Parity.None;
                case SerialParity.Even: return Parity.Even;
                case SerialParity.Odd: return Parity.Odd;
                case SerialParity.Mark: return Parity.Mark;
                case SerialParity.Space: return Parity.Space;
                default: return Parity.None;
            }
        }
    }
}
