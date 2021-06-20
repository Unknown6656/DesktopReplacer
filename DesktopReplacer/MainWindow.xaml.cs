using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows;

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Text;
using System.IO;
using System;

using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.Win32;

using Unknown6656.Common;
using Unknown6656.Controls.Console;
using Unknown6656.IO;

using DesktopReplacer.Widgets;

using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace DesktopReplacer
{
    public unsafe partial class MainWindow
        : Window
    {
        #region FIELDS / PROPERTIES

        private const string EXPLORER = "explorer";

        public static readonly DependencyProperty LoadedWidgetsProperty = DependencyProperty.Register(nameof(LoadedWidgets), typeof(object[]), typeof(MainWindow), new PropertyMetadata(null, (d, e) =>
        {
            if (d is MainWindow { widget_manager: var mgr } && Find<ItemsControl>(mgr) is ItemsControl ic)
                ic.ItemsSource = (IEnumerable)e.NewValue;
        }));

        private static readonly DirectoryInfo widget_dir = new(Assembly.GetExecutingAssembly().Location + "/../widgets");
        private static readonly FileInfo widget_settings_file = new(widget_dir.FullName + "/settings.json");

        private readonly List<DesktopIcon> _icons = new();
        private WinForms.Form? _list_container = null;


        public object[]? LoadedWidgets
        {
            get => GetValue(LoadedWidgetsProperty) as object[];
            set => SetValue(LoadedWidgetsProperty, value);
        }

        public IEnumerable<DesktopIcon> SelectedDesktopIcons => _icons.Where(d => d.IsSelected);

        #endregion
        #region .CTOR / .DTOR

        public MainWindow()
        {
            InitializeComponent();
            RestartExplorer();
            GetDesktopWindow();

            if (!widget_settings_file.Exists)
            {
                using FileStream fs = widget_settings_file.Create();
                using StreamWriter wr = new(fs);

                wr.Write("{}");
                wr.Flush();
                wr.Close();
                fs.Close();
            }
        }

        ~MainWindow() => RestoreDesktop();

        #endregion
        #region UI EVENT HANDLERS

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _hwnd_window = (void*)new WindowInteropHelper(this).Handle;

                HijackDesktop();
                UpdateBackground();
                UpdateScreenLayout();
                UpdateListview();
                LoadWidgets();

                SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
                SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            }
            catch (Exception? ex)
            when (!Debugger.IsAttached)
            {
                StringBuilder sb = new();

                while (ex != null)
                {
                    sb.Insert(0, $"[{ex.GetType()}] \"{ex.Message}\":\n{ex.StackTrace}\n");
                    ex = ex.InnerException;
                }

                MessageBox.Show(this, $"{sb}\nThe desktop replacer application will shut down now.", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Close();
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;

            RestoreDesktop();
            UnloadWidgets();

            Application.Current.Shutdown(0);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Blur_Click(object sender, RoutedEventArgs e) => AnimateBlur(30);

        private void Unblur_Click(object sender, RoutedEventArgs e) => AnimateBlur(0);

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateBackground();
            UpdateScreenLayout();
            UpdateListview();
            UnloadWidgets();
            LoadWidgets();
        }

        private void Icon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                FileSystemInfo[] selected = SelectedDesktopIcons.Select(i => i.AssociatedFile).Where(f => f is { }).ToArray()!;
                Point pos = PointToScreen(e.GetPosition(this));

                new ShellContextMenu().ShowContextMenu(selected, (int)pos.X, (int)pos.Y);
            }
        }

        private void Icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Icon_Click(object sender, RoutedEventArgs e)
        {
            // check if right click

            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                foreach (DesktopIcon i in _icons)
                    i.IsSelected = false;

            ((DesktopIcon)sender).IsSelected = true;
        }

        private void Icon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e) => UpdateScreenLayout();

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Desktop)
                UpdateBackground();
        }

        private void AnimateBlur(double target_radius)
        {
            img_wallpaper.BeginAnimation(MonitorBackground.BlurRadiusProperty, new DoubleAnimation(target_radius, new Duration(TimeSpan.FromMilliseconds(400))));
            // img_wallpaper.BlurRadius = target_radius;
        }

        #endregion
        #region BACK-END

        private static void RestartExplorer()
        {
            try
            {
                Process[]? procs = Process.GetProcessesByName(EXPLORER);

                if (procs.Length == 0)
                {
                    using Process? p = new()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = EXPLORER,
                            UseShellExecute = false,
                            RedirectStandardError = false,
                            RedirectStandardOutput = false,
                        }
                    };

                    p.Start();
                    Thread.Sleep(300); // TODO : fix this dogshite
                }
            }
            catch
            {
            }
        }

        private static BitmapSource CreateImageSource(Drawing.Bitmap bmp)
        {
            IntPtr handle = bmp.GetHbitmap();

            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                Win32.DeleteObject(handle);
            }
        }

        private void UpdateScreenLayout()
        {
            foreach (UIElement img in canvas.Children.Cast<UIElement>().Where(i => i is MonitorBackground).ToArray())
                canvas.Children.Remove(img);

            MonitorInfo[] screens = Desktop.FetchMonitors();
            double left = double.PositiveInfinity;
            double top = double.PositiveInfinity;

            foreach (MonitorInfo monitor in screens)
            {
                left = Math.Min(left, area.Left);
                top = Math.Min(top, area.Top);
            }

            Console.WriteLine($"-----------------------\nx: {left}  y: {top}");

            foreach ((Drawing.Rectangle area, bool primary, string name) in screens)
            {
                Console.WriteLine($"{primary}: {area}");

                FrameworkElement elem = grid_main;
                MonitorInfo mon = new()
                {
                    Width = area.Width,
                    Height = area.Height,
                    Left = area.Left - left,
                    Top = area.Top - top,
                    Frequency = Win32.GetDisplayRefreshRate(name),
                    Name = name,
                };

                if (primary)
                    img_wallpaper.Monitor = mon;
                else
                {
                    elem = new MonitorBackground(mon);

                    BindingOperations.SetBinding(elem, MonitorBackground.ImageSourceProperty, new Binding
                    {
                        Source = img_wallpaper,
                        Path = new PropertyPath(MonitorBackground.ImageSourceProperty),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    BindingOperations.SetBinding(elem, MonitorBackground.BlurRadiusProperty, new Binding
                    {
                        Source = img_wallpaper,
                        Path = new PropertyPath(MonitorBackground.BlurRadiusProperty),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });

                    canvas.Children.Add(elem);
                }

                elem.Width = mon.Width;
                elem.Height = mon.Height;
                elem.SetValue(Canvas.LeftProperty, mon.Left);
                elem.SetValue(Canvas.TopProperty, mon.Top);
                elem.SetValue(Canvas.RightProperty, mon.Right);
                elem.SetValue(Canvas.BottomProperty, mon.Bottom);

                Console.WriteLine($"  {elem}:  {area.Left - left} {area.Top - top}");
            }

            Console.WriteLine(canvas.Children.Count);

            InvalidateVisual();
        }

        private void UpdateBackground() => img_wallpaper.ImageSource = CreateImageSource(Desktop.FetchDesktopImage());

        private void UpdateListview()
        {
            if (Desktop.FetchDesktopIcons() is (RawIconInfo[] icons, double icon_size, ViewMdode view_mode))
            {
                double width = icon_size + 60;
                double height = icon_size + 50;

                desktop_icons.Children.Clear();
                _icons.Clear();

                foreach (RawIconInfo raw_icon in icons)
                {
                    DesktopIcon icon = new()
                    {
                        RawIcon = raw_icon,
                        AssociatedFile = raw_icon.MatchingFiles.FirstOrDefault(),
                        Text = raw_icon.DisplayName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
                            || raw_icon.DisplayName.EndsWith(".url", StringComparison.OrdinalIgnoreCase) ? raw_icon.DisplayName[..^4] : raw_icon.DisplayName,
                        IconSize = icon_size,
                        Width = width,
                        Height = height,
                        XPos = raw_icon.X * width,
                        YPos = raw_icon.Y * height,
                    };

                    _icons.Add(icon);

                    if (raw_icon.MatchingFiles.Length > 0 && raw_icon.MatchingFiles[0] is { } file)
                    {
                        icon.IsHidden = file.Attributes.HasFlag(FileAttributes.Hidden);

                        if (file is DirectoryInfo)
                        {
                            // TODO
                        }
                        else
                            using (ShellFile shell_file = ShellFile.FromFilePath(file.FullName))
                                icon.Icon = CreateImageSource(shell_file.Thumbnail.ExtraLargeBitmap);
                    }

                    icon.MouseDoubleClick += Icon_MouseDoubleClick;
                    icon.Click += Icon_Click;
                    icon.MouseDown += Icon_MouseDown;
                    icon.MouseUp += Icon_MouseUp;

                    desktop_icons.Children.Add(icon);
                }
            }
        }

        private void LoadWidgets()
        {
            Dictionary<string, object?>? settings = DataStream.FromFile(widget_settings_file).ToJSON<Dictionary<string, object?>>();
            List<object> w = new();

            foreach (UIElement holder in widgets.Children.Cast<UIElement>().ToArray())
                if (holder is DesktopWidgetHolder && holder != widget_manager)
                    widgets.Children.Remove(holder);

            foreach (AbstractDesktopWidget widget in WidgetLoader.LoadWidgetsFromDirectory(settings, widget_dir.FullName))
            {
                w.Add(new { widget.WidgetName, widget.WidgetVersion });
                widgets.Children.Add(new DesktopWidgetHolder
                {
                    Widget = widget
                });
            }

            LoadedWidgets = w.ToArray();
        }

        private void UnloadWidgets()
        {
            AbstractDesktopWidget[] ws = (from object holder in widgets.Children
                                          where holder is DesktopWidgetHolder { Widget: { } }
                                          select ((DesktopWidgetHolder)holder).Widget!).ToArray();
            Dictionary<string, object?> settings = WidgetLoader.UnloadWidgets(ws);

            DataStream.FromObjectAsJSON(settings).ToFile(widget_settings_file);

            LoadedWidgets = Array.Empty<object>();
        }

#if IGNORE
        public void GetDesktopIcons(void* hwnd)
        {


            var icon_count = SendMessage(_hwnd_listview, LVM_GETITEMCOUNT, null, null);
            var icon_pos = new (int x, int y)[icon_count];

            // GetDesktopIcons(_hwnd_listview);

            // set : SendMessage(_desktopHandle, LVM_SETITEMPOSITION, iconIndex, MakeLParam(x, y));
            //       SHChangeNotify(0x8000000, 0x1000, null, null);
            // get :



            //    fixed ((int x, int y)* ptr = icon_pos)
            //    for (int i = 0; i < icon_count; ++i)
            //        SendMessage(_hwnd_listview, LVM_GETITEMPOSITION, (void*)i, ptr + i);


            const int BUFFER_SIZE = 0x110;

            var icon_count = SendMessage(hwnd, LVM_GETITEMCOUNT, null, null);

            GetWindowThreadProcessId(hwnd, out int pid);

            void* vProcess = OpenProcess(0x00000038, false, pid);
            void* vPointer = VirtualAllocEx(vProcess, null, BUFFER_SIZE, 0x3000, 4); // reserver/commit, read/write

            LVITEMA currentDesktopIcon = new LVITEMA();
            byte[] vBuffer = new byte[BUFFER_SIZE];

            fixed (byte* buffer = vBuffer)
                try
                {
                    LVITEMA remoteBufferDesktopIcon = new LVITEMA
                    {
                        mask = 0x00000001,
                        cchTextMax = vBuffer.Length - sizeof(LVITEMA),
                        pszText = (void*)(buffer + sizeof(LVITEMA))
                    };

                    for (int i = 0; i < icon_count; i++)
                    {
                        remoteBufferDesktopIcon.iItem = i;

                        WriteProcessMemory(hwnd, buffer, &remoteBufferDesktopIcon, sizeof(LVITEMA), out _);

                        SendMessage(hwnd, LVM_GETITEMW, (void*)i, buffer);
                        ReadProcessMemory(hwnd, buffer, vBuffer, sizeof(LVITEMA), out int read_bytes);

                        if (read_bytes != sizeof(LVITEMA))
                            throw new Exception("Read false amount of bytes.");

                        currentDesktopIcon = *(LVITEMA*)buffer;
                        ReadProcessMemory(hwnd, currentDesktopIcon.pszText, vBuffer, 260, out read_bytes);

                        string txt = Encoding.Unicode.GetString(vBuffer, 0, read_bytes) + '\0';

                        txt = txt[..txt.IndexOf('\0')];

                        // TODO: Do something with the icon title.
                    }
                }
                finally
                {
                    VirtualFreeEx(vProcess, buffer, 0, 0x8000); // release
                    CloseHandle(vProcess);
                }
        }
#endif

        private void RestoreDesktop()
        {
            if (_hwnd_listview is null)
                return;

            Win32.SetParent(_hwnd_listview, _hwnd_desktop);
            _list_container?.Close();
            _list_container?.Dispose();
            _list_container = null;
            _hwnd_listview = null;
        }

        public static T? Find<T>(DependencyObject? parent)
            where T : DependencyObject
        {
            if (parent is { })
            {
                int cc = VisualTreeHelper.GetChildrenCount(parent);

                for (int i = 0; i < cc; ++i)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T t)
                        return t;
                    else if (Find<T>(child) is { } c)
                        return c;
                }
            }

            return null;
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct LVITEMA
    {
        public int mask;
        public int iItem;
        public int iSubItem;
        public int state;
        public int stateMask;
        public void* pszText;
        public int cchTextMax;
        public int iImage;
        public void* lParam;
        public int iIndent;
        public uint iGroupId;
        public void* puColumns;
    }
}
