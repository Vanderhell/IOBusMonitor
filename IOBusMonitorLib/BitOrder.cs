namespace IOBusMonitorLib
{
    /// <summary>
    /// Endianness used when combining two Modbus registers
    /// (word-swap for 32-bit / 64-bit values).
    /// </summary>
    public enum BitOrder : int
    {
        /// <summary>
        /// High-word first, low-word second (default).
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Low-word first, high-word second.
        /// </summary>
        Swapped = 1
    }
}
