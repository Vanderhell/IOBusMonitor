using System;
using System.Globalization;
using System.Windows.Data;

namespace IOBusMonitor
{
    /// <summary>
    /// Multi-value converter that performs a logical AND operation on all input boolean values.
    /// Returns true only if all values are true.
    /// </summary>
    public class AndConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts multiple boolean values into a single boolean result using logical AND.
        /// </summary>
        /// <param name="values">Array of input values (expected to be of type bool).</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture information.</param>
        /// <returns>True if all boolean values are true; otherwise, false.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is bool b && !b)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// ConvertBack is not implemented for this converter.
        /// </summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetTypes">Array of target types (not used).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture information.</param>
        /// <returns>Throws NotImplementedException.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("AndConverter does not support ConvertBack.");
        }
    }
}
