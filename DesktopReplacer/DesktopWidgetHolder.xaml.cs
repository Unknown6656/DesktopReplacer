using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows;

using System.ComponentModel;
using System;

using DesktopReplacer.Widgets;


namespace DesktopReplacer
{
    [ContentProperty(nameof(Widget))]
    public partial class DesktopWidgetHolder
        : ContentControl
    {
        public static readonly DependencyProperty CanBeCollapsedProperty = DependencyProperty.Register(nameof(CanBeCollapsed), typeof(bool), typeof(DesktopWidgetHolder), new PropertyMetadata(true, CanBeCollapsedChanged));

        public static readonly DependencyProperty WidgetProperty = DependencyProperty.Register(nameof(Widget), typeof(AbstractDesktopWidget), typeof(DesktopWidgetHolder), new PropertyMetadata(null));



        public FrameworkElement? Triangle => Template.FindName("triangle", this) as FrameworkElement;

        public AbstractDesktopWidget? Widget
        {
            get => GetValue(WidgetProperty) as AbstractDesktopWidget;
            set
            {
                RegisterEventHandlers(Widget, false);
                SetValue(WidgetProperty, value);
                SetValue(ContentProperty, value);
                RegisterEventHandlers(value, true);
                ApplyTemplate();
                UpdateTriangleVisiblity();
            }
        }

        public bool CanBeCollapsed
        {
            get => (bool)GetValue(CanBeCollapsedProperty);
            set => SetValue(CanBeCollapsedProperty, value);
        }


        public DesktopWidgetHolder() => InitializeComponent();

        private void RegisterEventHandlers(AbstractDesktopWidget? widget, bool subscribe)
        {
            if (widget is { })
            {
                DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(AbstractDesktopWidget.IsWidgetCollapsedProperty, typeof(AbstractDesktopWidget));

                if (subscribe)
                    dpd.AddValueChanged(widget, OnWidgetCollapsedChanged);
                else
                    dpd.RemoveValueChanged(widget, OnWidgetCollapsedChanged);
            }
        }

        private void OnWidgetCollapsedChanged(object? sender, EventArgs e)
        {
            if ((sender as AbstractDesktopWidget ?? Widget) is AbstractDesktopWidget w && Template.FindName("triangle_rot", this) is RotateTransform rot)
            {
                rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(w.IsWidgetCollapsed ? 180 : 0, new Duration(TimeSpan.FromMilliseconds(200))));

                if (Template.FindName("cc", this) is FrameworkElement cc)
                    cc.Visibility = w.IsWidgetCollapsed ? Visibility.Collapsed : Visibility.Visible;

                if (Template.FindName("cc_placeholder", this) is FrameworkElement ccp)
                    ccp.Visibility = w.IsWidgetCollapsed ? Visibility.Visible : Visibility.Collapsed;

                if (Triangle is FrameworkElement triangle)
                    triangle.ToolTip = (w.IsWidgetCollapsed ? "Show " : "Hide ") + w.WidgetName;
            }
        }

        private void Triangle_Click(object sender, RoutedEventArgs e)
        {
            if (Widget is AbstractDesktopWidget w)
                w.IsWidgetCollapsed ^= true;
        }

        private void UpdateTriangleVisiblity()
        {
            if (Triangle is FrameworkElement triangle)
                triangle.Visibility = (Widget?.Content is null || !CanBeCollapsed) ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void CanBeCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DesktopWidgetHolder dwh)
                dwh.UpdateTriangleVisiblity();
        }
    }
}
