using System;
using System.Threading.Tasks;
using unknown6656;

namespace Widgets.Essentials
{
    [WidgetInfo("Clock", "1.0")]
    public unsafe partial class Clock
    {
        private static readonly DateTime _unix0 = new DateTime(1970, 1, 1);


        public Clock() => InitializeComponent();

        public override void OnLoad()
        {
        }

        public override void OnUnload()
        {
        }

        public override Task OnTick()
        {
            DateTime now = DateTime.Now;
            long unix = (long)(now - _unix0).TotalSeconds;

            lb_time.Text = now.ToString("HH:mm:ss");
            lb_day.Text = now.ToString("dddd, MMM dd, yyyy");
            lb_unix.Text = $"0x{unix:x8} ({unix})";

            return Task.CompletedTask;
        }
    }
}
