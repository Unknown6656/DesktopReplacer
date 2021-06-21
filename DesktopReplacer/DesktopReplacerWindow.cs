using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Web.WebView2.Core;

using Unknown6656.IO;

namespace DesktopReplacer
{
    public sealed partial class DesktopReplacerWindow
        : Form
    {
        private static readonly DirectoryInfo DIR = AppDomain.CurrentDomain.SetupInformation.ApplicationBase is string dir ? new DirectoryInfo(dir) : new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
        private static readonly DirectoryInfo USER_DATA_DIR = DIR.CreateSubdirectory("webview2-cache");


        public DesktopWindow DesktopWindow { get; }
        public MonitorInfo[] Monitors { get; private set; }


        public DesktopReplacerWindow()
        {
            InitializeComponent();

            DesktopWindow = Desktop.GetDesktopWindow();
            Monitors = Array.Empty<MonitorInfo>();

            AllowTransparency = true;
            TransparencyKey = Color.Transparent;

            Load += DesktopReplacerWindow_Load;
        }

        ~DesktopReplacerWindow() => DesktopWindow.Restore();

        private void Close_Click(object sender, EventArgs e) => Close();

        private void DesktopReplacerWindow_Load(object? sender, EventArgs e)
        {
            // CoreWebView2Environment environment = await FetchWW2Environment();
            // CoreWebView2Controller controller = await environment.CreateCoreWebView2ControllerAsync(Handle);

            browser.Source = new("https://google.com/");


            UpdateDekstop();

            DesktopWindow.Hijack(this);
        }

        private static async Task<CoreWebView2Environment> FetchWW2Environment()
        {
            CoreWebView2Environment? environment = null;
retry:
            try
            {
                environment ??= await CoreWebView2Environment.CreateAsync(null, USER_DATA_DIR.FullName, new CoreWebView2EnvironmentOptions()
                {
                    // TODO
                });
            }
            catch (Exception)
            {
                DialogResult result = MessageBox.Show(@"
The WebView2 runtime could not be found. How would like to proceed?

[Retry]    Download the WebView2 runtime from Microsoft and try again (https://go.microsoft.com/fwlink/p/?LinkId=2124703).
[Ignore]  Restart the application.
[Abort]    Quit the application.
", "WebView2 Runtime not found", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2);

                if (result is DialogResult.Abort)
                    Application.Exit();
                else if (result is DialogResult.Retry)
                {
                    FileInfo ww2runtimeinstaller = new(Path.Combine(DIR.FullName, "webview2-runtime-installer.temp.exe"));

                    DataStream.FromHTTP("https://go.microsoft.com/fwlink/p/?LinkId=2124703").ToFile(ww2runtimeinstaller);

                    using (Process proc = Process.Start(ww2runtimeinstaller.FullName))
                        proc.WaitForExit();
                }

                goto retry;
            }

            return environment;
        }

        private void UpdateDekstop()
        {
            Monitors = Desktop.FetchMonitors();

            int m_bottom = int.MinValue;
            int m_right = int.MinValue;
            int m_left = int.MaxValue;
            int m_top = int.MaxValue;

            foreach (MonitorInfo monitor in Monitors)
            {
                m_bottom = Math.Max(m_bottom, monitor.Bottom);
                m_right = Math.Max(m_right, monitor.Right);
                m_left = Math.Min(m_left, monitor.Left);
                m_top = Math.Min(m_top, monitor.Top);
            }

            Top = m_top;
            Left = m_left;
            Width = m_right - m_left;
            Height = m_bottom - m_top;

            browser.Left = 0;
            browser.Top = 0;
            browser.Width = Width;
            browser.Height = Height;
            browser.SendToBack();
        }
    }
}
