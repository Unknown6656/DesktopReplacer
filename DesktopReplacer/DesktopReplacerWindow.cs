using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System;

using CefSharp.WinForms;

namespace DesktopReplacer
{
    public sealed class DesktopReplacerWindow
        : Form
    {
        public DesktopWindow DesktopWindow { get; }
        public ChromiumWebBrowser Browser { get; }


        public DesktopReplacerWindow()
        {
            DesktopWindow = Desktop.GetDesktopWindow();
            Browser = new("https://unknown6656.com/");
            Controls.Add(Browser);

            Load += DesktopReplacerWindow_Load;
            FormClosing += DesktopReplacerWindow_FormClosing;
        }

        ~DesktopReplacerWindow() => DesktopWindow.Restore();

        private void DesktopReplacerWindow_FormClosing(object? sender, FormClosingEventArgs e)
        {
        }

        private void DesktopReplacerWindow_Load(object? sender, EventArgs e)
        {
            MonitorInfo[] monitors = Desktop.FetchMonitors();







            DesktopWindow.Hijack(this);
        }
    }
}
