using System.Windows.Forms;
using System;

namespace DesktopReplacer
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using DesktopReplacerWindow window = new();

            Application.EnableVisualStyles();
            Application.Run(window);
        }
    }
}
