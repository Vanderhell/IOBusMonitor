using System;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Utility methods that infer the Siemens PLC data type (and the matching
    /// CLR type) from a DB address string such as <c>"DB1.DBD0"</c> or
    /// <c>"DB5.DBX10.2"</c>.
    /// </summary>
    public static class SimensAddressHelper
    {
        /// <summary>
        /// Determines the <see cref="DataType"/> based on common DB prefixes:
        /// <list type="table">
        ///   <item><term>DBX</term><description>Bit</description></item>
        ///   <item><term>DBB</term><description>Byte</description></item>
        ///   <item><term>DBW</term><description>Word / Int16</description></item>
        ///   <item><term>DBD</term><description>REAL&nbsp;(float)</description></item>
        ///   <item><term>DBL</term><description>DOUBLE&nbsp;(64-bit)</description></item>
        /// </list>
        /// Returns <see cref="DataType.Unknown"/> if no pattern matches.
        /// </summary>
        public static DataType GetDataTypeFromAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return DataType.Unknown;

            string a = address.ToUpperInvariant();

            if (a.Contains("DBX")) return DataType.Bit;
            if (a.Contains("DBW")) return DataType.Int;
            if (a.Contains("DBB")) return DataType.Byte;
            if (a.Contains("DBD")) return DataType.Real;
            if (a.Contains("DBL")) return DataType.Double;

            return DataType.Unknown;
        }

        /// <summary>
        /// Maps the inferred <see cref="DataType"/> to the corresponding CLR type
        /// (<c>typeof(float)</c>, <c>typeof(byte)</c>, etc.).
        /// </summary>
        public static Type GetClrTypeFromAddress(string address)
        {
            switch (GetDataTypeFromAddress(address))
            {
                case DataType.Bit: return typeof(bool);
                case DataType.Int: return typeof(short);
                case DataType.Word: return typeof(ushort);
                case DataType.Real: return typeof(float);
                case DataType.Double: return typeof(double);
                case DataType.Byte: return typeof(byte);
                default: return typeof(object);
            }
        }
    }
}
