using System.Reflection;
using System.Windows;
using System.IO;
using System;

using CefSharp.Wpf;
using CefSharp;

namespace DesktopReplacer
{
    public unsafe partial class App
        : Application
    {
        private static readonly DirectoryInfo DIR = AppDomain.CurrentDomain.SetupInformation.ApplicationBase is string dir ? new DirectoryInfo(dir) : new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
        private static readonly string CEF_DIR = Path.Combine(DIR.FullName, Environment.Is64BitProcess ? "x64" : "x86") + '/';


        public App() => AppDomain.CurrentDomain.AssemblyResolve += Resolver;

        protected override void OnStartup(StartupEventArgs e)
        {
            Cef.Initialize(new CefSettings
            {
                BrowserSubprocessPath = CEF_DIR + "../CefSharp.BrowserSubprocess.exe",
                BackgroundColor = 0, // transparent
            }, performDependencyCheck: false, browserProcessHandler: null);

            // CefSharpSettings.LegacyJavascriptBindingEnabled = true;
            CefSharpSettings.ConcurrentTaskExecution = true;

            base.OnStartup(e);
        }

        private static Assembly? Resolver(object? sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("CefSharp"))
            {
                string name = args.Name.Split(new[] { ',' }, 2)[0];
                string path = $"{CEF_DIR}{name}.dll";

                if (File.Exists(path))
                    return Assembly.LoadFile(path);
            }

            return null;
        }
    }
}
