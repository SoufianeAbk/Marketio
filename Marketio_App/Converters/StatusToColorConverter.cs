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
                    OrderStatus.Pending => Color.FromArgb("#FFA500"),      // Oranje
                    OrderStatus.Processing => Color.FromArgb("#007AFF"),   // Blauw
                    OrderStatus.Shipped => Color.FromArgb("#3478F6"),      // Ligt Blauw
                    OrderStatus.Delivered => Color.FromArgb("#34C759"),    // Groen
                    OrderStatus.Cancelled => Color.FromArgb("#FF3B30"),    // Rood
                    _ => Color.FromArgb("#666666")                         // Grijs
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