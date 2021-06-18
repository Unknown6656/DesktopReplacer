using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Timers;
using System.Linq;
using System.IO;
using System;

using Newtonsoft.Json;

[assembly: XmlnsPrefix("https://unknown6656.com/", "unknown6656")]
[assembly: XmlnsDefinition("https://unknown6656.com/", "unknown6656")]

namespace unknown6656
{
    public abstract class AbstractDesktopWidget
        : ContentControl
    {
        public static readonly DependencyProperty IsWidgetCollapsedProperty = DependencyProperty.Register(nameof(IsWidgetCollapsed), typeof(bool), typeof(AbstractDesktopWidget), new PropertyMetadata(false));

        public static readonly DependencyProperty TickIntervalProperty = DependencyProperty.Register(nameof(TickInterval), typeof(int), typeof(AbstractDesktopWidget), new PropertyMetadata(1000, OnIntervalChanged));

        public static readonly DependencyProperty WidgetVersionProperty = DependencyProperty.Register(nameof(WidgetVersion), typeof(string), typeof(AbstractDesktopWidget), new PropertyMetadata("1.0.0.0"));

        public static readonly DependencyProperty WidgetNameProperty = DependencyProperty.Register(nameof(WidgetName), typeof(string), typeof(AbstractDesktopWidget), new PropertyMetadata("Desktop Widget"));

        public static readonly DependencyProperty IsWidgetLoadedProperty = DependencyProperty.Register(nameof(IsWidgetLoaded), typeof(bool), typeof(AbstractDesktopWidget), new PropertyMetadata(false));

        private readonly Timer _timer;


        internal string WidgetSettingsKey => (GetType().AssemblyQualifiedName ?? WidgetName ?? GetType().FullName ?? GetType().Name).ToLowerInvariant();

        public bool IsWidgetCollapsed
        {
            get => (bool)GetValue(IsWidgetCollapsedProperty);
            set => SetValue(IsWidgetCollapsedProperty, value);
        }

        public bool IsWidgetLoaded
        {
            get => (bool)GetValue(IsWidgetLoadedProperty);
            private set => SetValue(IsWidgetLoadedProperty, value);
        }

        public string? WidgetName
        {
            get => GetValue(WidgetNameProperty) as string;
            set => SetValue(WidgetNameProperty, value);
        }

        public string? WidgetVersion
        {
            get => GetValue(WidgetVersionProperty) as string;
            set => SetValue(WidgetVersionProperty, value);
        }

        public int TickInterval
        {
            get => (int)GetValue(TickIntervalProperty);
            set => SetValue(TickIntervalProperty, value);
        }


        public AbstractDesktopWidget()
        {
            if (GetType().GetCustomAttribute<WidgetInfo>() is WidgetInfo info)
            {
                WidgetName = info.Name;
                WidgetVersion = info.Version;
            }
            else
                throw new TypeLoadException($"The widget '{GetType()}' must be decorated with an '{typeof(WidgetInfo)}'-attribute.");

            _timer = new Timer
            {
                Enabled = true
            };

            TickInterval = 1000;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        internal async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!IsWidgetLoaded)
            {
                OnLoad();

                _timer.Interval = TickInterval;
                _timer.Elapsed += _timer_Elapsed;
                _timer.Start();
            }

            IsWidgetLoaded = true;

            if (TickInterval >= 1000)
                await Dispatcher.InvokeAsync(OnTick);
        }

        internal void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (IsWidgetLoaded)
            {
                _timer.Stop();
                _timer.Elapsed -= _timer_Elapsed;

                OnUnload();
            }

            IsWidgetLoaded = false;
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e) => await Dispatcher.InvokeAsync(OnTick);

        public abstract void OnLoad();

        public abstract void OnUnload();

        public abstract Task OnTick();

        private static void OnIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AbstractDesktopWidget @this)
                @this._timer.Interval = (int)e.NewValue;
        }
    }

    public abstract class AbstractDesktopWidgetWithSettings<TSettings>
        : AbstractDesktopWidget
        where TSettings : class
    {
        public abstract TSettings DefaultSettings { get; }
        public TSettings CurrentSettings { get; internal set; }


        public AbstractDesktopWidgetWithSettings() => CurrentSettings = DefaultSettings;

        public sealed override void OnLoad() => OnLoad(CurrentSettings);

        public sealed override void OnUnload()
        {
            TSettings settings = CurrentSettings;

            OnUnload(ref settings);

            CurrentSettings = settings;
        }

        public abstract void OnLoad(TSettings settings);

        public abstract void OnUnload(ref TSettings settings);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class WidgetInfo
        : Attribute
    {
        public string Name { get; }
        public string? Version { get; }


        public WidgetInfo(string name)
            : this(name, null)
        {
        }

        public WidgetInfo(string name, string? version)
        {
            Name = name;
            Version = version;
        }
    }

    [WidgetInfo("Delegate Desktop Widget")]
    public class DesktopWidget
        : AbstractDesktopWidget
    {
        public static readonly DependencyProperty LoopActionProperty = DependencyProperty.Register(nameof(LoopAction), typeof(Action), typeof(DesktopWidget), new PropertyMetadata(null));


        public Action? LoopAction
        {
            get => GetValue(LoopActionProperty) as Action;
            set => SetValue(LoopActionProperty, value);
        }


        public override void OnLoad()
        {
        }

        public override void OnUnload()
        {
        }

        public override async Task OnTick()
        {
            if (LoopAction is { } act)
                await Task.Factory.StartNew(act).ConfigureAwait(false);
        }
    }

    public static class WidgetLoader
    {
        private static readonly HashSet<string> _extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".dll", ".ocx", ".exe", ".bin", ".dat", ".sys", ".com", ".widget" };
        private static readonly Assembly _current_assembly = Assembly.GetExecutingAssembly();


        public static AbstractDesktopWidget[] LoadWidgetsFromDirectory(Dictionary<string, object?> settings, string widget_dir)
        {
            if (new DirectoryInfo(widget_dir) is { Exists: true } dir)
            {
                static Assembly? tryfetch(FileInfo file)
                {
                    try
                    {
                        return Assembly.LoadFrom(file.FullName);
                    }
                    catch
                    {
                    }

                    return null;
                }

                return (from file in dir.EnumerateFiles()
                        where _extensions.Contains(file.Extension)
                        let asm = tryfetch(file)
                        where asm is { }
                        where asm != _current_assembly
                        from widget in LoadWidgets(settings, asm)
                        select widget).ToArray();
            }

            return new AbstractDesktopWidget[0];
        }

        public static AbstractDesktopWidget[] LoadWidgets(Dictionary<string, object?> settings, params Assembly[] assemblies)
        {
            Type @base = typeof(AbstractDesktopWidget);
            AbstractDesktopWidget load_settings(AbstractDesktopWidget widget)
            {
                if (GetSettingsType(widget) is Type target && settings.TryGetValue(widget.WidgetSettingsKey, out object? data))
                {
                    data = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data), target); // force convert types by named equality

                    if (data is { })
                        TrySetSettings(widget, data);
                }

                return widget;
            }

            return (from asm in assemblies
                    from t in asm.GetTypes()
                    where t.Assembly != _current_assembly
                    where @base.IsAssignableFrom(t)
                    where t != @base
                    let ins = Activator.CreateInstance(t) as AbstractDesktopWidget
                    where ins is { }
                    select load_settings(ins)).ToArray();
        }

        public static Dictionary<string, object?> UnloadWidgets(params AbstractDesktopWidget[] widgets)
        {
            Dictionary<string, object?> settings = new Dictionary<string, object?>();

            foreach (AbstractDesktopWidget? widget in widgets)
                if (widget is { })
                {
                    widget.OnUnloaded(widget, new RoutedEventArgs());

                    if (GetSettingsType(widget) is { })
                        settings[widget.WidgetSettingsKey] = TryGetSettings(widget);
                }

            return settings;
        }

        private static Type? GetSettingsType(AbstractDesktopWidget widget)
        {
            Type type = widget.GetType();

            while (type != typeof(object))
            {
                type = type.BaseType ?? type;

                if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() is Type gen && gen == typeof(AbstractDesktopWidgetWithSettings<>))
                    return type.GetGenericArguments().FirstOrDefault();
            }

            return null;
        }

        private static object? TryGetSettings(AbstractDesktopWidget widget) => widget.GetType().GetProperty(nameof(AbstractDesktopWidgetWithSettings<object>.CurrentSettings))?.GetValue(widget);

        private static void TrySetSettings(AbstractDesktopWidget widget, object settings)
        {
            if (widget.GetType().GetProperty(nameof(AbstractDesktopWidgetWithSettings<object>.CurrentSettings)) is PropertyInfo prop)
                try
                {
                    prop.SetValue(widget, settings);
                }
                catch
                {
                    MessageBox.Show($"Unable to load widget settings for '{widget.WidgetName}' (version {widget.WidgetVersion}).\nThe settings have been reset.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
        }
    }
}
