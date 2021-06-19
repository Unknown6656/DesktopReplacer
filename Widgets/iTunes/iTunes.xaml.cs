using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System;

using iTunesLib;

namespace DesktopReplacer.Widgets.iTunes
{
    [WidgetInfo(nameof(iTunes), "0.9")]
    public unsafe partial class iTunes
        : AbstractDesktopWidget
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(void* hWnd, int cmdShow);

        [DllImport("user32.dll")]
        private static extern int SendMessage(void* hWnd, uint Msg, void* wParam, void* lParam);


        public static readonly DependencyProperty ITStateProperty = DependencyProperty.Register(nameof(ITState), typeof(iTunesState), typeof(iTunes), new PropertyMetadata(null));

        private IiTunes? _itunes = null;


        public iTunesState? ITState
        {
            get => GetValue(ITStateProperty) as iTunesState;
            set => SetValue(ITStateProperty, value);
        }

        public Process? iTunesProcess => Process.GetProcessesByName("itunes").FirstOrDefault(p => p.MainWindowTitle == "iTunes");

        public void* iTunesHwnd => iTunesProcess?.MainWindowHandle is IntPtr p ? (void*)p : null;


        public iTunes() => InitializeComponent();

        public override void OnLoad()
        {
        }

        public override void OnUnload()
        {
        }

        public override Task OnTick()
        {
            if (iTunesProcess is null)
                _itunes = null;
            else
                _itunes ??= new iTunesAppClass();

            IITTrack? track = _itunes?.CurrentTrack;
            ITPlayButtonState ppst = ITPlayButtonState.ITPlayButtonStateStopDisabled;
            double prog = track?.Duration is int d ? _itunes!.PlayerPosition / (double)d : 0;

            static string printtime(int seconds) => TimeSpan.FromSeconds(seconds).ToString("c");


            _itunes?.GetPlayerButtonsState(out _, out ppst, out _);

            //if ((int)ppst > 3)
            //    track = null;

            ITState = new iTunesState
            {
                Progress = prog,
                Name = track?.Name ?? "----",
                Artist = track?.Artist ?? "----",
                Album = track?.Album ?? "----",
                Year = track?.Year.ToString() ?? "----",
                Genre = track?.Genre ?? "----",
                Time = track is null || _itunes is null ? "no playback" : $"{printtime(_itunes.PlayerPosition)} / {printtime(track.Duration)}",
            };

            return Task.CompletedTask;
        }

        private void Button_iTunesPrevious_Click(object sender, RoutedEventArgs e) => _itunes?.PreviousTrack();

        private void Button_iTunesBackward_Click(object sender, RoutedEventArgs e)
        {
            if (_itunes is { } i)
                i.PlayerPosition -= 15;
        }

        private void Button_iTunesPause_Click(object sender, RoutedEventArgs e) => _itunes?.PlayPause();

        private void Button_iTunesStop_Click(object sender, RoutedEventArgs e) => _itunes?.Stop();

        private void Button_iTunesForward_Click(object sender, RoutedEventArgs e)
        {
            if (_itunes is { } i)
                i.PlayerPosition += 15;
        }

        private void Button_iTunesNext_Click(object sender, RoutedEventArgs e) => _itunes?.NextTrack();

        private void Button_iTunesRandom_Click(object sender, RoutedEventArgs e)
        {
            if (_itunes is { CurrentPlaylist: var cp })
                cp.Shuffle ^= true;
        }

        private void Button_iTunesLoop_Click(object sender, RoutedEventArgs e)
        {
            if (_itunes is { CurrentPlaylist: var cp })
                cp.SongRepeat = (ITPlaylistRepeatMode)(((int)cp.SongRepeat + 1) % 2);
        }

        private void Button_iTunesRestore_Click(object sender, RoutedEventArgs e) => ShowWindow(iTunesHwnd, 5);

        private void Button_iTunesExit_Click(object sender, RoutedEventArgs e) => SendMessage(iTunesHwnd, 0x0010, null, null);
    }

    public class iTunesState
    {
        public double Progress { get; set; }
        public string? Name { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? Year { get; set; }
        public string? Genre { get; set; }
        public string? Time { get; set; }
    }
}
