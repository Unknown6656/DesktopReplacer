using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System;

using CefSharp.WinForms;
using CefSharp;

namespace DesktopReplacer
{
    public static class Program
    {
        private const string CEF_ASMPREFIX = "CefSharp";
        private const string CEF_SUBPROCESS = "CefSharp.BrowserSubprocess.exe";

        private static readonly DirectoryInfo DIR = AppDomain.CurrentDomain.SetupInformation.ApplicationBase is string dir ? new DirectoryInfo(dir) : new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
        private static readonly string CEF_DIR = Path.Combine(DIR.FullName, Environment.Is64BitProcess ? "x64" : "x86");


        public static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                if (args.Name.StartsWith(CEF_ASMPREFIX, StringComparison.InvariantCulture))
                {
                    string name = args.Name.Split(new[] { ',' }, 2)[0];
                    string path = $"{CEF_DIR}/{name}.dll";

                    if (File.Exists(path))
                        return Assembly.LoadFile(path);
                }

                return null;
            };


            _ = Cef.Initialize(new CefSettings
            {
                // BrowserSubprocessPath = Path.Combine(DIR.FullName, CEF_SUBPROCESS),
                BackgroundColor = 0, // transparent
            }, performDependencyCheck: false, browserProcessHandler: null);

            // CefSharpSettings.LegacyJavascriptBindingEnabled = true;
            CefSharpSettings.ConcurrentTaskExecution = true;


            Application.EnableVisualStyles();

            using (DesktopReplacerWindow window = new())
                Application.Run(window);

            Cef.Shutdown();
        }
    }
}
