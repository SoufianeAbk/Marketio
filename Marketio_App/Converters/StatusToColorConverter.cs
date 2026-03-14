using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Marketio_Shared.Enums;

namespace Marketio_App.Converters
{
    internal class StatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is OrderStatus status || (value is string statusString && Enum.TryParse<OrderStatus>(statusString, out status)))
            {
                return status switch
                {
                    OrderStatus.Pending => Color.FromArgb("#FFA500"),      // Orange
                    OrderStatus.Processing => Color.FromArgb("#007AFF"),   // Blue
                    OrderStatus.Shipped => Color.FromArgb("#3478F6"),      // Light Blue
                    OrderStatus.Delivered => Color.FromArgb("#34C759"),    // Green
                    OrderStatus.Cancelled => Color.FromArgb("#FF3B30"),    // Red
                    _ => Color.FromArgb("#666666")                         // Gray
                };
            }

            return Color.FromArgb("#666666");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}