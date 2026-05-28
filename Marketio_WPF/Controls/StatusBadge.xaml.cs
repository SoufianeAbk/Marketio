using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Marketio_WPF.Controls
{
    public partial class StatusBadge : UserControl
    {
        // DependencyProperty: Status
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(string),
                typeof(StatusBadge),
                new PropertyMetadata(string.Empty, OnStatusChanged));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public StatusBadge()
        {
            InitializeComponent();
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBadge badge)
                badge.ApplyStatus(e.NewValue as string);
        }

        private void ApplyStatus(string? status)
        {
            var (bg, fg, label) = status?.ToLowerInvariant() switch
            {
                "pending" => ("#F39C12", "#FFFFFF", "⏳ Pending"),
                "shipped" => ("#3498DB", "#FFFFFF", "🚚 Shipped"),
                "delivered" => ("#27AE60", "#FFFFFF", "✅ Delivered"),
                "cancelled" => ("#E74C3C", "#FFFFFF", "❌ Cancelled"),
                _ => ("#95A5A6", "#FFFFFF", status ?? "–")
            };

            BadgeBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(bg));
            BadgeText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(fg));
            BadgeText.Text = label;
        }
    }
}
