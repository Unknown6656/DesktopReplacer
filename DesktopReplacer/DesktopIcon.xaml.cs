using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopReplacer
{
    public partial class DesktopIcon
        : Button
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(DesktopIcon), new PropertyMetadata(null));

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(DesktopIcon), new PropertyMetadata(false));

        public static readonly DependencyProperty AssociatedFileProperty = DependencyProperty.Register(nameof(AssociatedFile), typeof(FileSystemInfo), typeof(DesktopIcon), new PropertyMetadata(null));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(DesktopIcon), new PropertyMetadata("<icon>"));

        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(DesktopIcon), new PropertyMetadata(50d, (s, e) => ((DesktopIcon)s).IconSizeChanged(e)));

        public static readonly DependencyProperty IsHiddenProperty = DependencyProperty.Register(nameof(IsHidden), typeof(bool), typeof(DesktopIcon), new PropertyMetadata(false));

        public static readonly DependencyProperty RawIconProperty = DependencyProperty.Register(nameof(RawIcon), typeof(RawIconInfo), typeof(DesktopIcon), new PropertyMetadata(null));


        public bool IsHidden
        {
            get => (bool)GetValue(IsHiddenProperty);
            set => SetValue(IsHiddenProperty, value);
        }

        public FileSystemInfo? AssociatedFile
        {
            get => GetValue(AssociatedFileProperty) as FileSystemInfo;
            set => SetValue(AssociatedFileProperty, value);
        }

        public string? Text
        {
            get => GetValue(TextProperty) as string;
            set => SetValue(TextProperty, value);
        }

        public ImageSource? Icon
        {
            get => GetValue(IconProperty) as ImageSource;
            set => SetValue(IconProperty, value);
        }

        public RawIconInfo RawIcon
        {
            get => (RawIconInfo)GetValue(RawIconProperty);
            set => SetValue(RawIconProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public double XPos
        {
            get => (double)GetValue(Canvas.LeftProperty);
            set => SetValue(Canvas.LeftProperty, value);
        }

        public double YPos
        {
            get => (double)GetValue(Canvas.TopProperty);
            set => SetValue(Canvas.TopProperty, value);
        }


        //public DesktopIcon() => InitializeComponent();

        private void IconSizeChanged(DependencyPropertyChangedEventArgs e)
        {
            double size = (double)e.NewValue;

            MinWidth = size + 20; // 10
            MinHeight = size + 20; // 30
        }
    }
}
