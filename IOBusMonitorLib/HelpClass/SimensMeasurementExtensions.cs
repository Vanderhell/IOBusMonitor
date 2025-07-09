using System;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Convenience helpers for <see cref="SimensMeasurement"/> that derive
    /// PLC-specific or CLR data-type information from the address string.
    /// </summary>
    public static class SimensMeasurementExtensions
    {
        /// <summary>
        /// Returns the inferred Siemens <see cref="DataType"/> based on the
        /// <c>DBx.DBy</c> address format stored in <paramref name="m"/>.
        /// </summary>
        public static DataType GetInferredDataType(this SimensMeasurement m)
        {
            return SimensAddressHelper.GetDataTypeFromAddress(m.Address);
        }

        /// <summary>
        /// Returns the corresponding .NET <see cref="System.Type"/> for the
        /// measurement’s address (e.g.&nbsp;<c>typeof(float)</c> for REAL).
        /// </summary>
        public static Type GetClrType(this SimensMeasurement m)
        {
            return SimensAddressHelper.GetClrTypeFromAddress(m.Address);
        }
    }
}
