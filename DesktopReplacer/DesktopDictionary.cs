using System.Drawing;
using System.Linq;
using System.IO;
using System;

using Unknown6656.Controls.Console;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

namespace DesktopReplacer
{
    public struct DesktopInfo
    {
        internal int Version;
        public string LinkedKey;
        public WorkspaceInfo[] Workspaces;

        public override string ToString() => $"{LinkedKey}: {Workspaces.Length} workspace(s)";
    }

    public struct WorkspaceInfo
    {
        internal int Version;
        public Point LinkShift;
        public Size GridSize;
        public int InfoFlags;
        public IconInfo[] Icons;

        public override string ToString() => $"[Shift={LinkShift}, Size={GridSize}, Flags={InfoFlags:x4}] {Icons.Length} icon(s)";
    }

    public struct IconInfo
    {
        public float X;
        public float Y;
        internal ushort NameIndex;
        public string DisplayName;
        public FileSystemInfo[] MatchingFiles;

        public override string ToString() => $"({X}|{Y}) {DisplayName}: '{string.Join<FileSystemInfo>("', '", MatchingFiles)}'";
    }

    public struct DesktopDictionary
    {
        internal int BagVersion;
        internal int TableVersion;
        public string[] IconNames;
        public DesktopInfo[] Desktops;


        public override string ToString() => $"{IconNames.Length} icon(s), {Desktops.Length} dekstop(s)";

        public static unsafe DesktopDictionary Read(byte[] raw_bytes, FileSystemInfo[] desktop_files)
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



            raw_bytes.HexDump();

            DesktopDictionary desktop = new();

            skip(16);

            desktop.BagVersion = read<int>();
            desktop.TableVersion = read<int>();
            desktop.IconNames = new string[read<long>()];

            (string name, int x, int y, FileSystemInfo? path)[] ics = new (string, int, int, FileSystemInfo?)[desktop.IconNames.Length];

            for (int i = 0; i < desktop.IconNames.Length; ++i)
                desktop.IconNames[i] = read_bstr();

            desktop.Desktops = new DesktopInfo[read<long>()];

            for (int i = 0; i < desktop.Desktops.Length; ++i)
            {
                DesktopInfo dinfo = new();

                dinfo.Version = read<int>();

                if (dinfo.Version != 0x10002)
                    continue;

                dinfo.LinkedKey = read_bstr();
                dinfo.Workspaces = new WorkspaceInfo[read<long>()];

                for (int j = 0; j < dinfo.Workspaces.Length; ++j)
                {
                    WorkspaceInfo workspace = new();

                    workspace.Version = read<int>();

                    if (workspace.Version != 0x10002)
                        continue;

                    workspace.LinkShift.X = read<int>();
                    workspace.LinkShift.Y = read<int>();
                    workspace.GridSize.Width = read<int>();
                    workspace.GridSize.Height = read<int>();
                    workspace.InfoFlags = read<int>();
                    workspace.Icons = new IconInfo[read<long>()];

                    for (int k = 0; k < workspace.Icons.Length; ++k)
                    {
                        IconInfo icon = new();

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
    }
}
