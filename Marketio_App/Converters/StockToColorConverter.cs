using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Marketio_App.Converters
{
    internal class StockToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                if (stock == 0)
                {
                    return Color.FromArgb("#FF3B30");  // Red - Out of stock
                }
                else if (stock < 10)
                {
                    return Color.FromArgb("#FFA500");  // Orange - Low stock
                }
                else
                {
                    return Color.FromArgb("#34C759");  // Green - In stock
                }
            }

            return Color.FromArgb("#666666");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}