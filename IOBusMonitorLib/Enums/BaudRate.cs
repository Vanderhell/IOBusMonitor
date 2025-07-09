namespace IOBusMonitorLib
{
    /// <summary>
    /// Common serial-port baud-rate settings.  
    /// The numeric value of each member equals the baud rate in bit/s.
    /// </summary>
    public enum BaudRate
    {
        /// <summary>1 200 bit/s</summary>
        Baud1200 = 1200,

        /// <summary>2 400 bit/s</summary>
        Baud2400 = 2400,

        /// <summary>4 800 bit/s</summary>
        Baud4800 = 4800,

        /// <summary>9 600 bit/s (default for many Modbus devices)</summary>
        Baud9600 = 9600,

        /// <summary>19 200 bit/s</summary>
        Baud19200 = 19200,

        /// <summary>38 400 bit/s</summary>
        Baud38400 = 38400,

        /// <summary>57 600 bit/s</summary>
        Baud57600 = 57600,

        /// <summary>115 200 bit/s</summary>
        Baud115200 = 115200,

        /// <summary>230 400 bit/s</summary>
        Baud230400 = 230400,

        /// <summary>460 800 bit/s</summary>
        Baud460800 = 460800,

        /// <summary>921 600 bit/s</summary>
        Baud921600 = 921600
    }
}
