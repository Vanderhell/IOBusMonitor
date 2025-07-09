namespace IOBusMonitorLib
{
    /// <summary>
    /// Identifies the protocol family of a device.
    /// </summary>
    public enum DeviceType : int
    {
        /// <summary>Device communicating over Modbus-TCP.</summary>
        ModbusTCP = 0,

        /// <summary>Device communicating over Modbus-RTU (serial).</summary>
        ModbusRTU = 1,

        /// <summary>Siemens S7 PLC.</summary>
        S7 = 2
    }
}
