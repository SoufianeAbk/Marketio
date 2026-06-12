using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Marketio_WPF.Converters
{
    /// <summary>
    /// Zet een tekenreekswaarde om naar zichtbaarheid.
    /// Lege of null-tekenreeksen worden Collapsed, niet-lege tekenreeksen worden Visible.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrEmpty(str))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}