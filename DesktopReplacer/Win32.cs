using System.Runtime.InteropServices;
using System.Text;
using System;

namespace DesktopReplacer
{
    internal unsafe delegate bool EnumWindowsProc(void* hWnd, void* lParam);

    public unsafe delegate int HookProc(int code, void* wParam, void* lParam);

    public unsafe delegate void HookEventHandler(object sender, HookEventArgs e);


    internal static unsafe class Win32
    {
        private const string GDI32_DLL = "gdi32.dll";
        private const string SHELL32_DLL = "shell32.dll";
        private const string KERNEL32_DLL = "kernel32.dll";
        private const string USER32_DLL = "user32.dll";
        private const string OLE32_DLL = "ole32.dll";
        private const string COREDLL_DLL = "coredll.dll";
        private const string SHLWAPI_DLL = "shlwapi.dll";

        internal const int LVM_GETITEMW = 0x104B;
        internal const int LVM_GETITEMCOUNT = 0x1004;
        internal const int LVM_SETITEMPOSITION = 0x100f;
        internal const int LVM_GETITEMPOSITION = 0x1010;
        internal const uint CMD_FIRST = 1;
        internal const uint CMD_LAST = 30000;


        [DllImport(USER32_DLL)]
        internal static extern void* GetShellWindow();

        [DllImport(USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void* SetParent(void* hWndChild, void* hWndNewParent);

        [DllImport(USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void* GetParent(void* hWnd);

        [DllImport(USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void* FindWindowEx(void* hwndParent, void* hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport(USER32_DLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, void* lParam);

        [DllImport(USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetClassName(void* hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport(USER32_DLL, SetLastError = true)]
        internal static extern bool GetWindowRect(void* hwnd, RECT* lpRect);

        [DllImport(GDI32_DLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(nint hObject);

        [DllImport(SHELL32_DLL)]
        internal static extern int SHChangeNotify(int eventId, int flags, void* wParam, void* lParam);

        [DllImport(USER32_DLL)]
        internal static extern int SendMessage(void* hWnd, uint Msg, void* wParam, void* lParam);

        [DllImport(USER32_DLL, SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(void* hWnd, out int lpdwProcessId);

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        internal static extern void* OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport(COREDLL_DLL, SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(void* hObject);

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        internal static extern void* VirtualAllocEx(void* hProcess, void* lpAddress, uint dwSize, int flAllocationType, int flProtect);

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        internal static unsafe extern bool VirtualFreeEx(void* hProcess, byte* pAddress, int size, int freeType);

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        internal static extern bool WriteProcessMemory(void* hProcess, void* lpBaseAddress, void* lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        internal static extern bool ReadProcessMemory(void* hProcess, void* lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport(USER32_DLL)]
        internal static extern nint GetWindowLong(void* hWnd, int nIndex);

        [DllImport(USER32_DLL, SetLastError = true)]
        internal static extern void* SetWindowLong(void* hWnd, int nIndex, nint dwNewLong);

        [DllImport(KERNEL32_DLL, EntryPoint = "SetLastError")]
        internal static extern void SetLastError(int dwErrorCode);

        [DllImport(SHELL32_DLL)]
        internal static extern int SHGetDesktopFolder(out void* ppshf);

        [DllImport(OLE32_DLL)]
        internal static extern void CoTaskMemFree(void* ptr);

        [DllImport(OLE32_DLL)]
        internal static extern void* CoTaskMemAlloc(int size);

        [DllImport(SHLWAPI_DLL, ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int StrRetToBuf(void* pstr, void* pidl, StringBuilder pszBuf, int cchBuf);

        [DllImport(USER32_DLL, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern uint TrackPopupMenuEx(void* hmenu, TPM flags, int x, int y, void* hwnd, void* lptpm);

        [DllImport(USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void* CreatePopupMenu();

        [DllImport(USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool DestroyMenu(void* hMenu);

        [DllImport(USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetMenuDefaultItem(void* hMenu, bool fByPos, uint gmdiFlags);

        [DllImport(USER32_DLL)]
        internal static extern void* SetWindowsHookEx(HookType code, HookProc func, void* hInstance, int threadID);

        [DllImport(USER32_DLL)]
        internal static extern int UnhookWindowsHookEx(void* hhook);

        [DllImport(USER32_DLL)]
        internal static extern int CallNextHookEx(void* hhook, int code, void* wParam, void* lParam);

        internal static T Cast<T>(void* ptr) => Cast<T>((nint)ptr);

        internal static T Cast<T>(nint ptr) => (T)Marshal.GetTypedObjectForIUnknown(ptr, typeof(T));
    }

    public sealed unsafe class HookEventArgs
        : EventArgs
    {
        public int HookCode;
        public void* wParam;
        public void* lParam;
    }

    public sealed unsafe class LocalWindowsHook
    {
        private readonly HookProc m_filterFunc;
        private readonly HookType m_hookType;
        private void* m_hhook;


        public event HookEventHandler? HookInvoked = delegate { };


        public LocalWindowsHook(HookType hook)
        {
            m_hookType = hook;
            m_filterFunc = CoreHookProc;
            m_hhook = null;
        }

        public LocalWindowsHook(HookType hook, HookProc func)
        {
            m_hookType = hook;
            m_filterFunc = func;
            m_hhook = null;
        }

#pragma warning disable CS0618 // obsolete warning for 'AppDomain.GetCurrentThreadId'
        public void Install() => m_hhook = Win32.SetWindowsHookEx(m_hookType, m_filterFunc, null, AppDomain.GetCurrentThreadId());
#pragma warning restore CS0618

        public void Uninstall() => Win32.UnhookWindowsHookEx(m_hhook);

        public void OnHookInvoked(HookEventArgs e) => HookInvoked?.Invoke(this, e);

        private int CoreHookProc(int code, void* wParam, void* lParam)
        {
            if (code < 0)
                return Win32.CallNextHookEx(m_hhook, code, wParam, lParam);

            // Let clients determine what to do
            HookEventArgs e = new()
            {
                HookCode = code,
                wParam = wParam,
                lParam = lParam
            };

            OnHookInvoked(e);

            return Win32.CallNextHookEx(m_hhook, code, wParam, lParam);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CWPSTRUCT
    {
        public void* lparam;
        public void* wparam;
        public int message;
        public void* hwnd;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal unsafe struct CMINVOKECOMMANDINFOEX
    {
        public int cbSize;
        public CMIC fMask;
        public void* hwnd;
        public void* lpVerb;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpDirectory;
        public SW nShow;
        public int dwHotKey;
        public void* hIcon;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpTitle;
        public void* lpVerbW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpParametersW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpDirectoryW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpTitleW;
        public POINT ptInvoke;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal unsafe struct MENUITEMINFO
    {
        public int cbSize;
        public MIIM fMask;
        public MFT fType;
        public MFS fState;
        public uint wID;
        public void* hSubMenu;
        public void* hbmpChecked;
        public void* hbmpUnchecked;
        public void* dwItemData;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string dwTypeData;
        public int cch;
        public void* hbmpItem;


        public MENUITEMINFO(string text)
        {
            cbSize = Marshal.SizeOf<CMINVOKECOMMANDINFOEX>();
            dwTypeData = text;
            cch = text.Length;
            fMask = 0;
            fType = 0;
            fState = 0;
            wID = 0;
            hSubMenu = null;
            hbmpChecked = null;
            hbmpUnchecked = null;
            dwItemData = null;
            hbmpItem = null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct STGMEDIUM
    {
        public TYMED tymed;
        public void* hBitmap;
        public void* hMetaFilePict;
        public void* hEnhMetaFile;
        public void* hGlobal;
        public void* lpszFileName;
        public void* pstm;
        public void* pstg;
        public void* pUnkForRelease;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal unsafe struct POINT
    {
        public int x;
        public int y;

        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [Flags]
    internal enum SHGNO
    {
        NORMAL = 0x0000,
        INFOLDER = 0x0001,
        FOREDITING = 0x1000,
        FORADDRESSBAR = 0x4000,
        FORPARSING = 0x8000
    }

    [Flags]
    internal enum SFGAO
        : uint
    {
        BROWSABLE = 0x8000000,
        CANCOPY = 1,
        CANDELETE = 0x20,
        CANLINK = 4,
        CANMONIKER = 0x400000,
        CANMOVE = 2,
        CANRENAME = 0x10,
        CAPABILITYMASK = 0x177,
        COMPRESSED = 0x4000000,
        CONTENTSMASK = 0x80000000,
        DISPLAYATTRMASK = 0xfc000,
        DROPTARGET = 0x100,
        ENCRYPTED = 0x2000,
        FILESYSANCESTOR = 0x10000000,
        FILESYSTEM = 0x40000000,
        FOLDER = 0x20000000,
        GHOSTED = 0x8000,
        HASPROPSHEET = 0x40,
        HASSTORAGE = 0x400000,
        HASSUBFOLDER = 0x80000000,
        HIDDEN = 0x80000,
        ISSLOW = 0x4000,
        LINK = 0x10000,
        NEWCONTENT = 0x200000,
        NONENUMERATED = 0x100000,
        READONLY = 0x40000,
        REMOVABLE = 0x2000000,
        SHARE = 0x20000,
        STORAGE = 8,
        STORAGEANCESTOR = 0x800000,
        STORAGECAPMASK = 0x70c50008,
        STREAM = 0x400000,
        VALIDATE = 0x1000000
    }

    [Flags]
    internal enum SHCONTF
    {
        FOLDERS = 0x0020,
        NONFOLDERS = 0x0040,
        INCLUDEHIDDEN = 0x0080,
        INIT_ON_FIRST_NEXT = 0x0100,
        NETPRINTERSRCH = 0x0200,
        SHAREABLE = 0x0400,
        STORAGE = 0x0800,
    }

    [Flags]
    internal enum CMF
        : uint
    {
        NORMAL = 0x00000000,
        DEFAULTONLY = 0x00000001,
        VERBSONLY = 0x00000002,
        EXPLORE = 0x00000004,
        NOVERBS = 0x00000008,
        CANRENAME = 0x00000010,
        NODEFAULT = 0x00000020,
        INCLUDESTATIC = 0x00000040,
        EXTENDEDVERBS = 0x00000100,
        RESERVED = 0xffff0000
    }

    [Flags]
    internal enum GCS
        : uint
    {
        VERBA = 0,
        HELPTEXTA = 1,
        VALIDATEA = 2,
        VERBW = 4,
        HELPTEXTW = 5,
        VALIDATEW = 6
    }

    [Flags]
    internal enum TPM
        : uint
    {
        LEFTBUTTON = 0x0000,
        RIGHTBUTTON = 0x0002,
        LEFTALIGN = 0x0000,
        CENTERALIGN = 0x0004,
        RIGHTALIGN = 0x0008,
        TOPALIGN = 0x0000,
        VCENTERALIGN = 0x0010,
        BOTTOMALIGN = 0x0020,
        HORIZONTAL = 0x0000,
        VERTICAL = 0x0040,
        NONOTIFY = 0x0080,
        RETURNCMD = 0x0100,
        RECURSE = 0x0001,
        HORPOSANIMATION = 0x0400,
        HORNEGANIMATION = 0x0800,
        VERPOSANIMATION = 0x1000,
        VERNEGANIMATION = 0x2000,
        NOANIMATION = 0x4000,
        LAYOUTRTL = 0x8000
    }

    internal enum CMD_CUSTOM
    {
        ExpandCollapse = (int)Win32.CMD_LAST + 1
    }

    [Flags]
    internal enum CMIC
        : uint
    {
        HOTKEY = 0x00000020,
        ICON = 0x00000010,
        FLAG_NO_UI = 0x00000400,
        UNICODE = 0x00004000,
        NO_CONSOLE = 0x00008000,
        ASYNCOK = 0x00100000,
        NOZONECHECKS = 0x00800000,
        SHIFT_DOWN = 0x10000000,
        CONTROL_DOWN = 0x40000000,
        FLAG_LOG_USAGE = 0x04000000,
        PTINVOKE = 0x20000000
    }

    [Flags]
    internal enum SW
    {
        HIDE = 0,
        SHOWNORMAL = 1,
        NORMAL = 1,
        SHOWMINIMIZED = 2,
        SHOWMAXIMIZED = 3,
        MAXIMIZE = 3,
        SHOWNOACTIVATE = 4,
        SHOW = 5,
        MINIMIZE = 6,
        SHOWMINNOACTIVE = 7,
        SHOWNA = 8,
        RESTORE = 9,
        SHOWDEFAULT = 10,
    }

    [Flags]
    internal enum WM
        : uint
    {
        ACTIVATE = 0x6,
        ACTIVATEAPP = 0x1C,
        AFXFIRST = 0x360,
        AFXLAST = 0x37F,
        APP = 0x8000,
        ASKCBFORMATNAME = 0x30C,
        CANCELJOURNAL = 0x4B,
        CANCELMODE = 0x1F,
        CAPTURECHANGED = 0x215,
        CHANGECBCHAIN = 0x30D,
        CHAR = 0x102,
        CHARTOITEM = 0x2F,
        CHILDACTIVATE = 0x22,
        CLEAR = 0x303,
        CLOSE = 0x10,
        COMMAND = 0x111,
        COMPACTING = 0x41,
        COMPAREITEM = 0x39,
        CONTEXTMENU = 0x7B,
        COPY = 0x301,
        COPYDATA = 0x4A,
        CREATE = 0x1,
        CTLCOLORBTN = 0x135,
        CTLCOLORDLG = 0x136,
        CTLCOLOREDIT = 0x133,
        CTLCOLORLISTBOX = 0x134,
        CTLCOLORMSGBOX = 0x132,
        CTLCOLORSCROLLBAR = 0x137,
        CTLCOLORSTATIC = 0x138,
        CUT = 0x300,
        DEADCHAR = 0x103,
        DELETEITEM = 0x2D,
        DESTROY = 0x2,
        DESTROYCLIPBOARD = 0x307,
        DEVICECHANGE = 0x219,
        DEVMODECHANGE = 0x1B,
        DISPLAYCHANGE = 0x7E,
        DRAWCLIPBOARD = 0x308,
        DRAWITEM = 0x2B,
        DROPFILES = 0x233,
        ENABLE = 0xA,
        ENDSESSION = 0x16,
        ENTERIDLE = 0x121,
        ENTERMENULOOP = 0x211,
        ENTERSIZEMOVE = 0x231,
        ERASEBKGND = 0x14,
        EXITMENULOOP = 0x212,
        EXITSIZEMOVE = 0x232,
        FONTCHANGE = 0x1D,
        GETDLGCODE = 0x87,
        GETFONT = 0x31,
        GETHOTKEY = 0x33,
        GETICON = 0x7F,
        GETMINMAXINFO = 0x24,
        GETOBJECT = 0x3D,
        GETSYSMENU = 0x313,
        GETTEXT = 0xD,
        GETTEXTLENGTH = 0xE,
        HANDHELDFIRST = 0x358,
        HANDHELDLAST = 0x35F,
        HELP = 0x53,
        HOTKEY = 0x312,
        HSCROLL = 0x114,
        HSCROLLCLIPBOARD = 0x30E,
        ICONERASEBKGND = 0x27,
        IME_CHAR = 0x286,
        IME_COMPOSITION = 0x10F,
        IME_COMPOSITIONFULL = 0x284,
        IME_CONTROL = 0x283,
        IME_ENDCOMPOSITION = 0x10E,
        IME_KEYDOWN = 0x290,
        IME_KEYLAST = 0x10F,
        IME_KEYUP = 0x291,
        IME_NOTIFY = 0x282,
        IME_REQUEST = 0x288,
        IME_SELECT = 0x285,
        IME_SETCONTEXT = 0x281,
        IME_STARTCOMPOSITION = 0x10D,
        INITDIALOG = 0x110,
        INITMENU = 0x116,
        INITMENUPOPUP = 0x117,
        INPUTLANGCHANGE = 0x51,
        INPUTLANGCHANGEREQUEST = 0x50,
        KEYDOWN = 0x100,
        KEYFIRST = 0x100,
        KEYLAST = 0x108,
        KEYUP = 0x101,
        KILLFOCUS = 0x8,
        LBUTTONDBLCLK = 0x203,
        LBUTTONDOWN = 0x201,
        LBUTTONUP = 0x202,
        LVM_GETEDITCONTROL = 0x1018,
        LVM_SETIMAGELIST = 0x1003,
        MBUTTONDBLCLK = 0x209,
        MBUTTONDOWN = 0x207,
        MBUTTONUP = 0x208,
        MDIACTIVATE = 0x222,
        MDICASCADE = 0x227,
        MDICREATE = 0x220,
        MDIDESTROY = 0x221,
        MDIGETACTIVE = 0x229,
        MDIICONARRANGE = 0x228,
        MDIMAXIMIZE = 0x225,
        MDINEXT = 0x224,
        MDIREFRESHMENU = 0x234,
        MDIRESTORE = 0x223,
        MDISETMENU = 0x230,
        MDITILE = 0x226,
        MEASUREITEM = 0x2C,
        MENUCHAR = 0x120,
        MENUCOMMAND = 0x126,
        MENUDRAG = 0x123,
        MENUGETOBJECT = 0x124,
        MENURBUTTONUP = 0x122,
        MENUSELECT = 0x11F,
        MOUSEACTIVATE = 0x21,
        MOUSEFIRST = 0x200,
        MOUSEHOVER = 0x2A1,
        MOUSELAST = 0x20A,
        MOUSELEAVE = 0x2A3,
        MOUSEMOVE = 0x200,
        MOUSEWHEEL = 0x20A,
        MOVE = 0x3,
        MOVING = 0x216,
        NCACTIVATE = 0x86,
        NCCALCSIZE = 0x83,
        NCCREATE = 0x81,
        NCDESTROY = 0x82,
        NCHITTEST = 0x84,
        NCLBUTTONDBLCLK = 0xA3,
        NCLBUTTONDOWN = 0xA1,
        NCLBUTTONUP = 0xA2,
        NCMBUTTONDBLCLK = 0xA9,
        NCMBUTTONDOWN = 0xA7,
        NCMBUTTONUP = 0xA8,
        NCMOUSEHOVER = 0x2A0,
        NCMOUSELEAVE = 0x2A2,
        NCMOUSEMOVE = 0xA0,
        NCPAINT = 0x85,
        NCRBUTTONDBLCLK = 0xA6,
        NCRBUTTONDOWN = 0xA4,
        NCRBUTTONUP = 0xA5,
        NEXTDLGCTL = 0x28,
        NEXTMENU = 0x213,
        NOTIFY = 0x4E,
        NOTIFYFORMAT = 0x55,
        NULL = 0x0,
        PAINT = 0xF,
        PAINTCLIPBOARD = 0x309,
        PAINTICON = 0x26,
        PALETTECHANGED = 0x311,
        PALETTEISCHANGING = 0x310,
        PARENTNOTIFY = 0x210,
        PASTE = 0x302,
        PENWINFIRST = 0x380,
        PENWINLAST = 0x38F,
        POWER = 0x48,
        PRINT = 0x317,
        PRINTCLIENT = 0x318,
        QUERYDRAGICON = 0x37,
        QUERYENDSESSION = 0x11,
        QUERYNEWPALETTE = 0x30F,
        QUERYOPEN = 0x13,
        QUEUESYNC = 0x23,
        QUIT = 0x12,
        RBUTTONDBLCLK = 0x206,
        RBUTTONDOWN = 0x204,
        RBUTTONUP = 0x205,
        RENDERALLFORMATS = 0x306,
        RENDERFORMAT = 0x305,
        SETCURSOR = 0x20,
        SETFOCUS = 0x7,
        SETFONT = 0x30,
        SETHOTKEY = 0x32,
        SETICON = 0x80,
        SETMARGINS = 0xD3,
        SETREDRAW = 0xB,
        SETTEXT = 0xC,
        SETTINGCHANGE = 0x1A,
        SHOWWINDOW = 0x18,
        SIZE = 0x5,
        SIZECLIPBOARD = 0x30B,
        SIZING = 0x214,
        SPOOLERSTATUS = 0x2A,
        STYLECHANGED = 0x7D,
        STYLECHANGING = 0x7C,
        SYNCPAINT = 0x88,
        SYSCHAR = 0x106,
        SYSCOLORCHANGE = 0x15,
        SYSCOMMAND = 0x112,
        SYSDEADCHAR = 0x107,
        SYSKEYDOWN = 0x104,
        SYSKEYUP = 0x105,
        TCARD = 0x52,
        TIMECHANGE = 0x1E,
        TIMER = 0x113,
        TVM_GETEDITCONTROL = 0x110F,
        TVM_SETIMAGELIST = 0x1109,
        UNDO = 0x304,
        UNINITMENUPOPUP = 0x125,
        USER = 0x400,
        USERCHANGED = 0x54,
        VKEYTOITEM = 0x2E,
        VSCROLL = 0x115,
        VSCROLLCLIPBOARD = 0x30A,
        WINDOWPOSCHANGED = 0x47,
        WINDOWPOSCHANGING = 0x46,
        WININICHANGE = 0x1A,
        SH_NOTIFY = 0x0401
    }

    [Flags]
    internal enum MFT
        : uint
    {
        GRAYED = 0x00000003,
        DISABLED = 0x00000003,
        CHECKED = 0x00000008,
        SEPARATOR = 0x00000800,
        RADIOCHECK = 0x00000200,
        BITMAP = 0x00000004,
        OWNERDRAW = 0x00000100,
        MENUBARBREAK = 0x00000020,
        MENUBREAK = 0x00000040,
        RIGHTORDER = 0x00002000,
        BYCOMMAND = 0x00000000,
        BYPOSITION = 0x00000400,
        POPUP = 0x00000010
    }

    [Flags]
    internal enum MFS
        : uint
    {
        GRAYED = 0x00000003,
        DISABLED = 0x00000003,
        CHECKED = 0x00000008,
        HILITE = 0x00000080,
        ENABLED = 0x00000000,
        UNCHECKED = 0x00000000,
        UNHILITE = 0x00000000,
        DEFAULT = 0x00001000
    }

    [Flags]
    internal enum MIIM
        : uint
    {
        BITMAP = 0x80,
        CHECKMARKS = 0x08,
        DATA = 0x20,
        FTYPE = 0x100,
        ID = 0x02,
        STATE = 0x01,
        STRING = 0x40,
        SUBMENU = 0x04,
        TYPE = 0x10
    }

    [Flags]
    internal enum TYMED
    {
        ENHMF = 0x40,
        FILE = 2,
        GDI = 0x10,
        HGLOBAL = 1,
        ISTORAGE = 8,
        ISTREAM = 4,
        MFPICT = 0x20,
        NULL = 0
    }

    public enum HookType
        : int
    {
        WH_JOURNALRECORD = 0,
        WH_JOURNALPLAYBACK = 1,
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        WH_HARDWARE = 8,
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }
}
