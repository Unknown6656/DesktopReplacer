using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using System.IO;
using System;

using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

using Unknown6656.IO;

namespace DesktopReplacer
{
    public sealed partial class DesktopReplacerWindow
        : Form
    {
        internal const string EXPLORER = "explorer";
        internal const string EDGE_ARGS = "--disable-web-security --allow-file-access-from-files --allow-file-access";
        internal static readonly DirectoryInfo DIR = AppDomain.CurrentDomain.SetupInformation.ApplicationBase is string dir ? new DirectoryInfo(dir) : new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
        internal static readonly DirectoryInfo RESOURCE_DIR = DIR.CreateSubdirectory("resources");
        internal static readonly DirectoryInfo USER_DATA_DIR = DIR.CreateSubdirectory("webview2-cache");


        public DesktopWindow DesktopWindow { get; }
        public MonitorInfo[] Monitors { get; private set; }
        public HTMLGenerator Generator { get; }
        public WebView2 Browser { get; }


        public DesktopReplacerWindow()
        {
            DesktopWindow = Desktop.GetDesktopWindow();
            Monitors = Array.Empty<MonitorInfo>();
            Generator = new();
            Browser = new();
            (Browser as ISupportInitialize)?.BeginInit();

            SuspendLayout();

            Browser.CreationProperties = null;
            Browser.Source = new("about:empty", UriKind.Absolute);
            Browser.ZoomFactor = 1D;


            Button button = new()
            {
                Location = new(200, 200),
                Size = new(100, 30),
                Text = "Close",
            };
            button.Click += (_, _) => Close();
            Controls.Add(button);
            button = new()
            {
                Location = new(200, 240),
                Size = new(100, 30),
                Text = "Redraw",
            };
            button.Click += (_, _) =>
            {
                Generator.ResetTemplateCache();
                UpdateDekstop();
            };
            Controls.Add(button);

            Controls.Add(Browser);

            AllowTransparency = true;
            DoubleBuffered = true;
            TransparencyKey = Color.Transparent;
            FormBorderStyle = FormBorderStyle.None;

            (Browser as ISupportInitialize)?.EndInit();

            ResumeLayout(false);

            Load += DesktopReplacerWindow_Load;
            FormClosing += DesktopReplacerWindow_FormClosing;
        }

        ~DesktopReplacerWindow() => DesktopWindow.Restore();

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e) => UpdateDekstop();

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category is UserPreferenceCategory.Desktop
                           or UserPreferenceCategory.General
                           or UserPreferenceCategory.Icon
                           or UserPreferenceCategory.Window
                           or UserPreferenceCategory.VisualStyle)
                UpdateDekstop();
        }

        private void DesktopReplacerWindow_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;

            DesktopWindow.Restore();
        }

        private async void DesktopReplacerWindow_Load(object? sender, EventArgs e)
        {
            CoreWebView2Environment environment = await FetchWW2Environment();

            await Browser.EnsureCoreWebView2Async(environment);

            DesktopWindow.Hijack(this);

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            await Task.Delay(300).ContinueWith(_ => UpdateDekstop());
        }

        private static async Task<CoreWebView2Environment> FetchWW2Environment()
        {
            CoreWebView2Environment? environment = null;
retry:
            try
            {
                environment ??= await CoreWebView2Environment.CreateAsync(null, USER_DATA_DIR.FullName, new CoreWebView2EnvironmentOptions()
                {
                    AdditionalBrowserArguments = EDGE_ARGS,
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

        private void UpdateDekstop() => Invoke(new MethodInvoker(delegate
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

            (RawIconInfo[], double, ViewMode)? icons = Desktop.FetchDesktopIcons();
            GenerationResult generated = Generator.GenerateDesktopHTMLCode(Bounds, Monitors, Desktop.FetchDesktopImagePath(), icons);

            Browser.Left = 0;
            Browser.Top = 0;
            Browser.Width = Width;
            Browser.Height = Height;
            Browser.SendToBack();

            try
            {
                Browser.Source = new Uri("file://" + generated.OutPath);
                Browser.Reload();
                // Browser.NavigateToString(generated.HTML);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }));

        public static async Task<bool> RestartExplorer()
        {
            try
            {
                Process[]? procs = Process.GetProcessesByName(EXPLORER);

                if (procs.Length is 0)
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

                    await Task.Delay(300); // TODO : fix this dogshite

                    return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
