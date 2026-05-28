using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Marketio_WPF.Controls
{
    public partial class StockIndicator : UserControl
    {
        // Drempelwaarden (aanpasbaar via XAML of code)
        public static readonly DependencyProperty LowThresholdProperty =
            DependencyProperty.Register(nameof(LowThreshold), typeof(int), typeof(StockIndicator),
                new PropertyMetadata(5));

        public static readonly DependencyProperty MediumThresholdProperty =
            DependencyProperty.Register(nameof(MediumThreshold), typeof(int), typeof(StockIndicator),
                new PropertyMetadata(20));

        // DependencyProperty: Stock
        public static readonly DependencyProperty StockProperty =
            DependencyProperty.Register(
                nameof(Stock),
                typeof(int),
                typeof(StockIndicator),
                new PropertyMetadata(0, OnStockChanged));

        public int Stock
        {
            get => (int)GetValue(StockProperty);
            set => SetValue(StockProperty, value);
        }

        public int LowThreshold
        {
            get => (int)GetValue(LowThresholdProperty);
            set => SetValue(LowThresholdProperty, value);
        }

        public int MediumThreshold
        {
            get => (int)GetValue(MediumThresholdProperty);
            set => SetValue(MediumThresholdProperty, value);
        }

        public StockIndicator()
        {
            InitializeComponent();
        }

        private static void OnStockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StockIndicator indicator)
                indicator.ApplyStock((int)e.NewValue);
        }

        private void ApplyStock(int stock)
        {
            string color;
            string label;

            if (stock == 0)
            {
                color = "#E74C3C"; // rood
                label = "Uitverkocht";
            }
            else if (stock <= LowThreshold)
            {
                color = "#E74C3C"; // rood
                label = $"{stock} (Laag)";
            }
            else if (stock <= MediumThreshold)
            {
                color = "#F39C12"; // oranje
                label = $"{stock} (Beperkt)";
            }
            else
            {
                color = "#27AE60"; // groen
                label = stock.ToString();
            }

            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            StatusDot.Fill = brush;
            StockText.Foreground = brush;
            StockText.Text = label;
        }
    }
}
