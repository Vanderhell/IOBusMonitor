namespace IOBusMonitorLib
{
    /// <summary>
    /// Siemens PLC CPU families supported by the application.  
    /// Numeric values correspond to those used by S7.Net.
    /// </summary>
    public enum CpuType : int
    {
        /// <summary>S7-200 family.</summary>
        S7200 = 0,

        /// <summary>Siemens LOGO! 0BA8.</summary>
        Logo0BA8 = 1,

        /// <summary>S7-200 SMART family.</summary>
        S7200Smart = 2,

        /// <summary>S7-300 family.</summary>
        S7300 = 10,

        /// <summary>S7-400 family.</summary>
        S7400 = 20,

        /// <summary>S7-1200 family.</summary>
        S71200 = 30,

        /// <summary>S7-1500 family.</summary>
        S71500 = 40
    }
}
