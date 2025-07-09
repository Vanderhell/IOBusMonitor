namespace IOBusMonitorLib
{
    /// <summary>
    /// Identifies the protocol family used by a measuring point.
    /// </summary>
    public enum PointType : int
    {
        /// <summary>Point read over Modbus-TCP.</summary>
        ModbusTCP = 0,

        /// <summary>Point read over Modbus-RTU (serial).</summary>
        ModbusRTU = 1,

        /// <summary>Point read from a Siemens S7 PLC.</summary>
        S7 = 2
    }
}
