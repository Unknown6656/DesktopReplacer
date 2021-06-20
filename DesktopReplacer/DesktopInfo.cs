using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using System.Linq;
using System.IO;
using System;

using Microsoft.Win32;

using Unknown6656.Common;

namespace DesktopReplacer
{
    public struct RawDesktopInfo
    {
        internal int Version;
        public string LinkedKey;
        public RawWorkspaceInfo[] Workspaces;

        public override string ToString() => $"{LinkedKey}: {Workspaces.Length} workspace(s)";
    }

    public struct RawWorkspaceInfo
    {
        internal int Version;
        public Point LinkShift;
        public Size GridSize;
        public int InfoFlags;
        public RawIconInfo[] Icons;

        public override string ToString() => $"[Shift={LinkShift}, Size={GridSize}, Flags={InfoFlags:x4}] {Icons.Length} icon(s)";
    }

    public struct RawIconInfo
    {
        public float X;
        public float Y;
        internal ushort NameIndex;
        public string DisplayName;
        public FileSystemInfo[] MatchingFiles;

        public override string ToString() => $"({X}|{Y}) {DisplayName}: '{string.Join<FileSystemInfo>("', '", MatchingFiles)}'";
    }

    public unsafe struct Desktop
    {
        private const string DESKTOP_IMAGE_PATH = "%AppData%/Microsoft/Windows/Themes/TranscodedWallpaper";
        private const string RKEY_DESKTOP = @"Software\Microsoft\Windows\Shell\Bags\1\Desktop";
        private const string RKEY_LOGICALVIEWMODE = "LogicalViewMode";
        private const string RKEY_ICONLAYOUT = "IconLayouts";
        private const string RKEY_ICONSIZE = "IconSize";
        private const string SHELLDLL_DEFVIEW = "SHELLDLL_DefView";
        private const string SHELLDLL_SYSLISTVIEW32 = "SysListView32";
        private const string SHELLDLL_FOLDERVIEW = "FolderView";
        private const string SHELLDLL_WORKERW = "WorkerW";


        internal int BagVersion;
        internal int TableVersion;
        public string[] IconNames;
        public RawDesktopInfo[] Desktops;


        public override string ToString() => $"{IconNames.Length} icon(s), {Desktops.Length} dekstop(s)";

        private static Desktop FetchDesktopIcons(byte[] raw_bytes, FileSystemInfo[] desktop_files)
        {
            int raw_index = 0;
            void skip(int count) => raw_index += count;
            T peek<T>() where T : unmanaged => *(T*)Unsafe.AsPointer(ref raw_bytes[raw_index]);
            T read<T>() where T : unmanaged
            {
                T value = peek<T>();

                skip(sizeof(T));

                return value;
            }
            string read_bstr()
            {
                long len = read<long>();

                if (len >= 0)
                {
                    char[] chars = new char[len];

                    for (int i = 0; i < chars.Length; ++i)
                        chars[i] = read<char>();

                    return new string(chars).TrimEnd('\0');
                }
                else
                    return "";
            }


            Desktop desktop = new();

            skip(16);

            desktop.BagVersion = read<int>();
            desktop.TableVersion = read<int>();
            desktop.IconNames = new string[read<long>()];

            (string name, int x, int y, FileSystemInfo? path)[] ics = new (string, int, int, FileSystemInfo?)[desktop.IconNames.Length];

            for (int i = 0; i < desktop.IconNames.Length; ++i)
                desktop.IconNames[i] = read_bstr();

            desktop.Desktops = new RawDesktopInfo[read<long>()];

            for (int i = 0; i < desktop.Desktops.Length; ++i)
            {
                RawDesktopInfo dinfo = new();

                dinfo.Version = read<int>();

                if (dinfo.Version != 0x10002)
                    continue;

                dinfo.LinkedKey = read_bstr();
                dinfo.Workspaces = new RawWorkspaceInfo[read<long>()];

                for (int j = 0; j < dinfo.Workspaces.Length; ++j)
                {
                    RawWorkspaceInfo workspace = new();

                    workspace.Version = read<int>();

                    if (workspace.Version != 0x10002)
                        continue;

                    workspace.LinkShift.X = read<int>();
                    workspace.LinkShift.Y = read<int>();
                    workspace.GridSize.Width = read<int>();
                    workspace.GridSize.Height = read<int>();
                    workspace.InfoFlags = read<int>();
                    workspace.Icons = new RawIconInfo[read<long>()];

                    for (int k = 0; k < workspace.Icons.Length; ++k)
                    {
                        RawIconInfo icon = new();

                        icon.X = read<float>();
                        icon.Y = read<float>();
                        icon.DisplayName = desktop.IconNames[read<ushort>()];
                        icon.MatchingFiles = desktop_files.Where(fi => icon.DisplayName.Equals(fi.Name, StringComparison.OrdinalIgnoreCase)).ToArray();

                        workspace.Icons[k] = icon;
                    }

                    dinfo.Workspaces[j] = workspace;
                }

                desktop.Desktops[i] = dinfo;
            }

            return desktop;
        }

        public static (RawIconInfo[] Icons, double IconSize, ViewMdode ViewMode)? FetchDesktopIcons()
        {
            FileSystemInfo[] desktop_files = new[]
            {
                Environment.SpecialFolder.DesktopDirectory,
                Environment.SpecialFolder.CommonDesktopDirectory,
            }.Select(p => new DirectoryInfo(Environment.GetFolderPath(p)))
            .SelectMany(dir => dir.EnumerateFileSystemInfos())
            .ToArray();

            if (Registry.CurrentUser.OpenSubKey(RKEY_DESKTOP) is RegistryKey rkey && (byte[]?)rkey.GetValue(RKEY_ICONLAYOUT, Array.Empty<byte>()) is byte[] raw)
                using (rkey)
                {
                    Desktop desktops = FetchDesktopIcons(raw, desktop_files);
                    ViewMdode mode = (ViewMdode)(int)(rkey.GetValue(RKEY_LOGICALVIEWMODE) ?? 3);
                    double icon_size = (int)(rkey.GetValue(RKEY_ICONSIZE) ?? 0) * 1.1;

                    rkey.Close();

                    return ((from desktop in desktops.Desktops
                             from workspace in desktop.Workspaces ?? Array.Empty<RawWorkspaceInfo>()
                             from icon in workspace.Icons ?? Array.Empty<RawIconInfo>()
                             select icon).ToArray(), icon_size, mode);
                }

            return null;
        }

        public static MonitorInfo[] FetchMonitors() => Screen.AllScreens.ToArray(s => new MonitorInfo(s));

        public static Bitmap FetchDesktopImage()
        {
            FileInfo fi = new(Environment.ExpandEnvironmentVariables(DESKTOP_IMAGE_PATH));

            return (Bitmap)Image.FromFile(fi.FullName);
        }

        public static DesktopWindow GetDesktopWindow()
        {
            void* _ProgMan = Win32.GetShellWindow();
            void* _SHELLDLL_DefViewParent = _ProgMan;
            void* _SHELLDLL_DefView = Win32.FindWindowEx(_ProgMan, null, SHELLDLL_DEFVIEW, null);
            void* _SysListView32 = Win32.FindWindowEx(_SHELLDLL_DefView, null, SHELLDLL_SYSLISTVIEW32, SHELLDLL_FOLDERVIEW);

            if (_SHELLDLL_DefView is null)
                _ = Win32.EnumWindows((hwnd, lParam) =>
                {
                    void* child;
                    StringBuilder sb = new(8);

                    Win32.GetClassName(hwnd, sb, sb.Capacity);

                    if (sb.ToString() == SHELLDLL_WORKERW && (child = Win32.FindWindowEx(hwnd, null, SHELLDLL_DEFVIEW, null)) != null)
                    {
                        _SHELLDLL_DefViewParent = hwnd;
                        _SHELLDLL_DefView = child;
                        _SysListView32 = Win32.FindWindowEx(child, null, SHELLDLL_SYSLISTVIEW32, SHELLDLL_FOLDERVIEW);

                        return false;
                    }
                    else
                        return true;
                }, null);

            return new(_SHELLDLL_DefView, _SysListView32);
        }
    }

    public sealed unsafe class DesktopWindow
    {
        private readonly object _mutex = new();

        public void* HwndDesktop { get; }
        public void* HwndListview { get; }
        public void* HwndContainer { get; private set; }
        public void* HwndHijacker { get; private set; }
        public bool HasBeenHijacked { get; private set; }
        public Form? Hijacker { get; private set; }


        internal DesktopWindow(void* hwnd_desktop, void* hwnd_listview)
        {
            HwndDesktop = hwnd_desktop;
            HwndListview = hwnd_listview;
            HwndContainer = null;
            HasBeenHijacked = false;
        }

        public void Hijack(Form hijacker)
        {
            lock (_mutex)
            {
                if (HasBeenHijacked)
                    return;

                Form iconlist_container = new()
                {
                    Width = 500,
                    Height = 500,
                    Opacity = 0,
                    Visible = false,
                };
                iconlist_container.Show();
                iconlist_container.Visible = false;

                HwndContainer = (void*)iconlist_container.Handle;
                HwndHijacker = (void*)hijacker.Handle;
                Hijacker = hijacker;

                RECT rect;

                Win32.SetParent(HwndListview, HwndContainer);
                Win32.SetParent(HwndHijacker, HwndDesktop);
                Win32.GetWindowRect(HwndDesktop, &rect);

                Hijacker.Width = rect.right - rect.left + 1;
                Hijacker.Height = rect.bottom - rect.top + 1;
                Hijacker.WindowState = FormWindowState.Maximized;

                nint exstl_win = Win32.GetWindowLong(HwndHijacker, -20) | 0x00000080; // WS_EX_TOOLWINDOW
                nint exstl_con = Win32.GetWindowLong(HwndContainer, -20) | 0x00000080;

                Win32.SetWindowLong(HwndHijacker, -20, exstl_win);
                Win32.SetWindowLong(HwndContainer, -20, exstl_con);

                HasBeenHijacked = true;
            }
        }

        public void Restore()
        {
            lock (_mutex)
                if (HasBeenHijacked)
                {
                    _ = Win32.SetParent(HwndListview, HwndDesktop);

                    HwndContainer = null;
                    HwndHijacker = null;
                    Hijacker = null;
                    HasBeenHijacked = false;
                }
        }
    }

    public sealed class MonitorInfo
    {
        public bool IsPrimary { get; }
        public string? Name { get; }
        public int Frequency { get; }
        public int Width { get; }
        public int Height { get; }
        public int Top { get; }
        public int Left { get; }
        public int Bottom => Top + Height;
        public int Right => Left + Width;


        internal MonitorInfo(Screen screen)
        {
            Rectangle area = screen.Bounds;

            Top = area.Top;
            Left = area.Left;
            Width = area.Width;
            Height = area.Height;
            Name = screen.DeviceName;
            IsPrimary = screen.Primary;
            Frequency = Win32.GetDisplayRefreshRate(Name);
        }
    }

    public enum ViewMdode
        : int
    {
        Details = 1,
        Tiles = 2,
        Icons = 3,
    }
}
