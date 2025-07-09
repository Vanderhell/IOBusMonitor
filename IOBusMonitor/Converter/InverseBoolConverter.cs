using System;
using System.Globalization;
using System.Windows.Data;

namespace IOBusMonitor
{
    /// <summary>
    /// Value converter that inverts a boolean value.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to its inverse.
        /// </summary>
        /// <param name="value">The original value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture information.</param>
        /// <returns>False if the input is true; true otherwise.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            // Default fallback when input is not a boolean
            return true;
        }

        /// <summary>
        /// Converts a boolean value back to its original value (also inversion).
        /// </summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetType">The type of the binding source property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture information.</param>
        /// <returns>False if the input is true; true otherwise.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            // Default fallback when input is not a boolean
            return true;
        }
    }
}
