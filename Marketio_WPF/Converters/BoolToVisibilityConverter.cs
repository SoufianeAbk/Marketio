using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Marketio_WPF.Converters
{
    /// <summary>
    /// Converts a boolean value to Visibility.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                return vis == Visibility.Visible;
            }
            return false;
        }
    }
}