using System.Globalization;
using System.Windows.Data;

namespace Marketio_WPF.Converters
{
    /// <summary>
    /// Inverts a boolean value.
    /// </summary>
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return true;
        }
    }
}