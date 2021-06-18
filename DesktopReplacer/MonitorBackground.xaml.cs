using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopReplacer
{
    public partial class MonitorBackground
        : UserControl
    {
        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(MonitorBackground), new PropertyMetadata(null));

        public static readonly DependencyProperty MonitorProperty = DependencyProperty.Register(nameof(Monitor), typeof(MonitorInfo), typeof(MonitorBackground));

        public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(nameof(BlurRadius), typeof(double), typeof(MonitorBackground), new PropertyMetadata(0d));


        public double BlurRadius
        {
            get => (double)GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        public ImageSource? ImageSource
        {
            get => GetValue(ImageSourceProperty) as ImageSource;
            set => SetValue(ImageSourceProperty, value);
        }

        public MonitorInfo? Monitor
        {
            get => GetValue(MonitorProperty) as MonitorInfo;
            set => SetValue(MonitorProperty, value);
        }


        public MonitorBackground()
        {
        }

        public MonitorBackground(MonitorInfo monitor)
        {
            InitializeComponent();

            Monitor = monitor;
        }
    }

    public class MonitorInfo
    {
        public string? Name { get; set; }
        public int Frequency { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public double Bottom => Top + Height;
        public double Right => Left + Width;
    }
}
