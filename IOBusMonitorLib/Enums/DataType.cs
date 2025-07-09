namespace IOBusMonitorLib
{
    /// <summary>
    /// PLC-level data types inferred from Siemens DB address
    /// or Modbus register size.
    /// </summary>
    public enum DataType
    {
        /// <summary>Single bit (BOOL).</summary>
        Bit,

        /// <summary>Signed 16-bit integer (INT).</summary>
        Int,

        /// <summary>Unsigned 16-bit (WORD).</summary>
        Word,

        /// <summary>IEEE-754 32-bit float (REAL).</summary>
        Real,

        /// <summary>IEEE-754 64-bit float (DOUBLE / LREAL).</summary>
        Double,

        /// <summary>Unsigned 8-bit value (BYTE).</summary>
        Byte,

        /// <summary>Type could not be determined.</summary>
        Unknown
    }
}
