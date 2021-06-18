using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace DesktopReplacer
{
    /// <summary>
    /// "Stand-alone" shell context menu
    /// 
    /// It isn't really debugged but is mostly working.
    /// Create an instance and call ShowContextMenu with a list of FileInfo for the files.
    /// Limitation is that it only handles files in the same directory but it can be fixed
    /// by changing the way files are translated into PIDLs.
    /// 
    /// Based on FileBrowser in C# from CodeProject
    /// http://www.codeproject.com/useritems/FileBrowser.asp
    /// 
    /// Hooking class taken from MSDN Magazine Cutting Edge column
    /// http://msdn.microsoft.com/msdnmag/issues/02/10/CuttingEdge/
    /// 
    /// Andreas Johansson
    /// afjohansson@hotmail.com
    /// http://afjohansson.spaces.live.com
    /// </summary>
    /// <example>
    ///    ShellContextMenu scm = new ShellContextMenu();
    ///    FileInfo[] files = new FileInfo[1];
    ///    files[0] = new FileInfo(@"c:\windows\notepad.exe");
    ///    scm.ShowContextMenu(this.Handle, files, Cursor.Position);
    /// </example>
    public unsafe sealed class ShellContextMenu
        : NativeWindow
    {
        #region FIELDS / CONSTANTS

        private const int MAX_PATH = 260;
        private const int S_OK = 0;
        private const int S_FALSE = 1;

        private static Guid IID_IShellFolder = new("{000214E6-0000-0000-C000-000000000046}");
        private static Guid IID_IContextMenu = new("{000214e4-0000-0000-c000-000000000046}");
        private static Guid IID_IContextMenu2 = new("{000214f4-0000-0000-c000-000000000046}");
        private static Guid IID_IContextMenu3 = new("{bcfce0a0-ec17-11d0-8d10-00a0c90f2719}");

        private IContextMenu? _oContextMenu;
        private IContextMenu2? _oContextMenu2;
        private IContextMenu3? _oContextMenu3;
        private IShellFolder? _oDesktopFolder;
        private IShellFolder? _oParentFolder;
        private string? _strParentFolder;
        private void*[]? _arrPIDLs;

        #endregion
        #region .CTOR / .DTOR

        public ShellContextMenu() => CreateHandle(new CreateParams());

        ~ShellContextMenu() => ReleaseAll();

        #endregion

        private void*[] GetPIDLs_internal(IShellFolder? folder, string[] paths)
        {
            if (folder is null)
                return new void*[0];

            void*[] arrPIDLs = new void*[paths.Length];

            for (int i = 0; i < paths.Length; i++)
            {
                int nResult = folder.ParseDisplayName(null, null, paths[i], out _, out void* pPIDL, out _);

                if (nResult != S_OK)
                {
                    FreePIDLs(arrPIDLs);

                    return new void*[0];
                }

                arrPIDLs[i] = pPIDL;
            }

            return arrPIDLs;
        }

        private void*[] GetPIDLs(IEnumerable<FileSystemInfo> files) => files.FirstOrDefault() switch
        {
            FileInfo file => file.DirectoryName,
            DirectoryInfo dir => dir.Parent.FullName,
            _ => null
        } is string folder ? GetPIDLs_internal(GetParentFolder(folder), files.Select(fi => fi.Name).ToArray()) : new void*[0];

        private void FreePIDLs(void*[]? pidls)
        {
            if (pidls is { Length: int l })
                for (int n = 0; n < l; n++)
                    if (pidls[n] != null)
                    {
                        Win32.CoTaskMemFree(pidls[n]);

                        pidls[n] = null;
                    }
        }

        private void ReleaseAll()
        {
            void release<T>(ref T? obj)
                where T : class
            {
                if (obj is { })
                {
                    Marshal.ReleaseComObject(obj);
                    obj = null;
                }
            }

            release(ref _oContextMenu);
            release(ref _oContextMenu2);
            release(ref _oContextMenu3);
            release(ref _oDesktopFolder);
            release(ref _oParentFolder);

            if (_arrPIDLs is { })
            {
                FreePIDLs(_arrPIDLs);
                _arrPIDLs = null;
            }
        }

        /// <summary>Gets the interfaces to the context menu</summary>
        /// <param name="parent">Parent folder</param>
        /// <param name="arrPIDLs">PIDLs</param>
        /// <returns>true if it got the interfaces, otherwise false</returns>
        private bool GetContextMenuInterfaces(IShellFolder? parent, void*[] arrPIDLs, out void* ctxMenuPtr)
        {
            ctxMenuPtr = null;
            _oContextMenu = null;

            if (parent?.GetUIObjectOf(null, (uint)arrPIDLs.Length, arrPIDLs, ref IID_IContextMenu, null, out ctxMenuPtr) == S_OK)
                _oContextMenu = Win32.Cast<IContextMenu>(ctxMenuPtr);

            return _oContextMenu != null;
        }

        private void InvokeCommand(IContextMenu? menu, uint nCmd, string? folder, int x, int y)
        {
            if (menu is null || folder is null)
                return;

            CMINVOKECOMMANDINFOEX invoke = new()
            {
                lpVerb = (void*)(nCmd - Win32.CMD_FIRST),
                lpDirectory = folder,
                lpVerbW = (void*)(nCmd - Win32.CMD_FIRST),
                lpDirectoryW = folder,
                fMask = CMIC.UNICODE | CMIC.PTINVOKE | ((Control.ModifierKeys & Keys.Control) != 0 ? CMIC.CONTROL_DOWN : 0) |
                                                       ((Control.ModifierKeys & Keys.Shift) != 0 ? CMIC.SHIFT_DOWN : 0),
                ptInvoke = new POINT(x, y),
                nShow = SW.SHOWNORMAL
            };

            invoke.cbSize = Marshal.SizeOf(invoke);
            menu.InvokeCommand(ref invoke);
        }

        private IShellFolder GetDesktopFolder()
        {
            if (_oDesktopFolder is null)
            {
                if (Win32.SHGetDesktopFolder(out void* pUnkownDesktopFolder) != S_OK)
                    throw new ShellContextMenuException("Failed to get the desktop shell folder.");

                _oDesktopFolder = Win32.Cast<IShellFolder>(pUnkownDesktopFolder);
            }

            return _oDesktopFolder;
        }

        private IShellFolder? GetParentFolder(string folderName)
        {
            if (_oParentFolder is null)
            {
                IShellFolder? desktop = GetDesktopFolder();
                int result = 0;

                if (desktop is null)
                    return null;

                result = desktop.ParseDisplayName(null, null, folderName, out _, out void* pidl, out _);

                if (result != S_OK)
                    return null;

                void* alloc = Win32.CoTaskMemAlloc(MAX_PATH * 2 + 4);
                *(int*)alloc = 0;

                result = desktop.GetDisplayNameOf(pidl, SHGNO.FORPARSING, alloc);

                StringBuilder foldername = new(MAX_PATH);

                Win32.StrRetToBuf(alloc, pidl, foldername, MAX_PATH);
                Win32.CoTaskMemFree(alloc);

                alloc = null;
                _strParentFolder = foldername.ToString();
                result = desktop.BindToObject(pidl, null, ref IID_IShellFolder, out void* parent);

                Win32.CoTaskMemFree(pidl);

                if (result != S_OK)
                    return null;

                _oParentFolder = Win32.Cast<IShellFolder>(parent);
            }

            return _oParentFolder;
        }

        /// <summary>
        /// This method receives WindowMessages. It will make the "Open With" and "Send To" work 
        /// by calling HandleMenuMsg and HandleMenuMsg2. It will also call the OnContextMenuMouseHover 
        /// method of Browser when hovering over a ContextMenu item.
        /// </summary>
        /// <param name="m">the Message of the Browser's WndProc</param>
        /// <returns>true if the message has been handled, false otherwise</returns>
        protected override void WndProc(ref Message m)
        {
            void* w = (void*)m.WParam;
            void* l = (void*)m.LParam;

            if ((_oContextMenu != null) &&
                (m.Msg == (int)WM.MENUSELECT) &&
                (HiWord((int)w) & (int)MFT.SEPARATOR) == 0 &&
                (HiWord((int)w) & (int)MFT.POPUP) == 0)
            {
                string info;

                if (LoWord((uint)w) == (uint)CMD_CUSTOM.ExpandCollapse)
                    info = "Expands or collapses the current selected item";
                else
                    info = "";

                // TODO : wtf?
            }

            if (_oContextMenu2 != null &&
                (m.Msg == (int)WM.INITMENUPOPUP ||
                 m.Msg == (int)WM.MEASUREITEM ||
                 m.Msg == (int)WM.DRAWITEM))
            {
                if (_oContextMenu2.HandleMenuMsg(m.Msg, w, l) == S_OK)
                    return;
            }

            if (_oContextMenu3 != null && m.Msg == (int)WM.MENUCHAR)
            {
                if (_oContextMenu3.HandleMenuMsg2((uint)m.Msg, w, l, null) == S_OK)
                    return;
            }

            base.WndProc(ref m);
        }

        private void InvokeContextMenuDefault(FileInfo[] files)
        {
            ReleaseAll();

            void* menu = null;

            try
            {
                _arrPIDLs = GetPIDLs(files);

                if (_arrPIDLs.Length == 0)
                    return;

                if (!GetContextMenuInterfaces(_oParentFolder, _arrPIDLs, out _))
                {
                    ReleaseAll();

                    return;
                }

                menu = Win32.CreatePopupMenu();

                int result = _oContextMenu?.QueryContextMenu(menu, 0, Win32.CMD_FIRST, Win32.CMD_LAST, CMF.DEFAULTONLY | ((Control.ModifierKeys & Keys.Shift) != 0 ? CMF.EXTENDEDVERBS : 0)) ?? S_FALSE;
                uint nDefaultCmd = (uint)Win32.GetMenuDefaultItem(menu, false, 0);
                Point pos = Control.MousePosition;

                if (nDefaultCmd >= Win32.CMD_FIRST)
                    InvokeCommand(_oContextMenu, nDefaultCmd, files[0].DirectoryName, pos.X, pos.Y);
            }
            finally
            {
                if (menu != null)
                    Win32.DestroyMenu(menu);

                ReleaseAll();
            }
        }

        /// <summary>
        /// Shows the context menu
        /// </summary>
        /// <param name="arrFI">FileInfos (should all be in same directory)</param>
        /// <param name="pointScreen">Where to show the menu</param>
        public void ShowContextMenu(IEnumerable<FileSystemInfo> files, int x, int y)
        {
            ReleaseAll();

            _arrPIDLs = GetPIDLs(files);

            if (_arrPIDLs.Length == 0)
                return;

            int result;
            void* menu = null;
            IntPtr ctx = default;
            IntPtr ctx2 = default;
            IntPtr ctx3 = default;

            try
            {
                if (GetContextMenuInterfaces(_oParentFolder, _arrPIDLs, out void* ptr))
                    ctx = (IntPtr)ptr;
                else
                {
                    ReleaseAll();

                    return;
                }

                menu = Win32.CreatePopupMenu();
                result = _oContextMenu?.QueryContextMenu(menu, 0, Win32.CMD_FIRST, Win32.CMD_LAST, CMF.EXPLORE | CMF.NORMAL | ((Control.ModifierKeys & Keys.Shift) != 0 ? CMF.EXTENDEDVERBS : 0)) ?? S_FALSE;

                Marshal.QueryInterface((IntPtr)ctx, ref IID_IContextMenu2, out ctx2);
                Marshal.QueryInterface((IntPtr)ctx, ref IID_IContextMenu3, out ctx3);

                _oContextMenu2 = Win32.Cast<IContextMenu2>(ctx2);
                _oContextMenu3 = Win32.Cast<IContextMenu3>(ctx3);

                uint selected = Win32.TrackPopupMenuEx(menu, TPM.RETURNCMD, x, y, (void*)Handle, null);

                Win32.DestroyMenu(menu);

                if (selected != 0)
                    InvokeCommand(_oContextMenu, selected, _strParentFolder, x, y);
            }
            finally
            {
                //hook.Uninstall();

                if (menu != null)
                    Win32.DestroyMenu(menu);

                Marshal.Release(ctx);
                Marshal.Release(ctx2);
                Marshal.Release(ctx3);

                ReleaseAll();
            }
        }

        private static int HiWord(int ptr) => (ptr & 0x80000000) == 0x80000000 ? ptr >> 16 : (ptr >> 16) & 0xffff;

        private static uint HiWord(uint ptr) => (ptr & 0x80000000) == 0x80000000 ? ptr >> 16 : LoWord(ptr >> 16);

        private static uint LoWord(uint ptr) => ptr & 0xffff;
    }

    public sealed class ShellContextMenuException
        : Exception
    {
        /// <summary>Default contructor</summary>
        public ShellContextMenuException()
        {
        }

        /// <summary>Constructor with message</summary>
        /// <param name="message">Message</param>
        public ShellContextMenuException(string message)
            : base(message)
        {
        }
    }
}
