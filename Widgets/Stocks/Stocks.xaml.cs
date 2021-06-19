// #define USE_BINDING

using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

using CefSharp;
using CefSharp.Wpf;

namespace DesktopReplacer.Widgets.Stocks
{
    [WidgetInfo(nameof(Stocks), "1.0")]
    public partial class Stocks
    {
#if USE_BINDING
        private readonly LoadedBinding _binding = new LoadedBinding { Loaded = false };
#endif


        public override StocksSettings DefaultSettings => new();


        public Stocks() => InitializeComponent();

        public override void OnLoad(StocksSettings settings)
        {
            const double scale_factor = .8;

#if USE_BINDING
            _binding.Unload();
#endif
            Height = settings.Height;
            wb.Height = settings.Height;
#if USE_BINDING
            wb.JavascriptObjectRepository.UnRegister(nameof(LoadedBinding));
            wb.JavascriptObjectRepository.Register(nameof(LoadedBinding), _binding, true);
#endif
            wb.ContextMenu = null;

            string html = $@"
<!DOCTYPE html>
<html lang=""en"">
    <head>
        <title>trading view</title>
        <style>
            * {{
                overflow: hidden !important;
            }}

            body,
            .tradingview-widget-container {{
                height: {wb.ActualHeight - 2:F2}px !important;
                width: {wb.ActualWidth - 2:F2}px !important;
                display: block;
                position: fixed !important;
                padding: 0 !important;
                margin-left: 0px !important;
                margin-top: 0px !important;
                left: 0px !important;
                top: 0px !important;
            }}

            #tradingview_18539 {{
                height: {wb.ActualHeight / scale_factor:F2}px !important;
                width: {wb.ActualWidth / scale_factor:F2}px !important;
                position: fixed !important;
                padding: 0 !important;
                margin: 0 !important;
                transform-origin: 0% 0% !important;
                transform: scale({scale_factor:F2}) !important;
            }}
        </style>
    </head>
    <body>
        <div class=""tradingview-widget-container"">
            <div id=""tradingview_18539""></div>
            <script type=""text/javascript"" src=""https://code.jquery.com/jquery-3.5.1.min.js""></script>
            <script type=""text/javascript"" src=""https://s3.tradingview.com/tv.js""></script>
            <script type=""text/javascript"">
                let widget = new TradingView.widget({{
                    ""autosize"": true,
                    // ""wdith"": {(int)(wb.ActualHeight / scale_factor)},
                    // ""height"": {(int)(wb.ActualWidth / scale_factor)},
                    ""symbol"": ""{settings.Symbol}"",
                    ""interval"": ""{settings.Interval}"",
                    ""timezone"": ""Europe/Berlin"",
                    ""theme"": ""dark"",
                    ""style"": ""{(int)settings.Style}"",
                    ""locale"": ""en"",
                    ""toolbar_bg"": ""#f1f3f6"",
                    ""enable_publishing"": false,
                    ""hide_top_toolbar"": true,
                    ""allow_symbol_change"": true,
                    ""save_image"": false,
                    ""container_id"": ""tradingview_18539""
                }});
";
#if USE_BINDING
            html += $@"
                (async function() {{
                    await CefSharp.BindObjectAsync(""{nameof(LoadedBinding)}"");
                    let promise = window.{nameof(LoadedBinding)}.markAsLoaded();

                    promise.then(function (data) {{
                        console.log(""success: "" + data);
                    }}, function (err) {{
                        console.log(""error: "" + err);
                    }});
                }})();
";
#endif
            html += $@"
            </script>
        </div>
    </body>
</html>
";

            wb.LoadHtml(html);
        }

        public override void OnUnload(ref StocksSettings settings)
        {
        }

        public override Task OnTick()
        {
#if USE_BINDING
            if (!_binding.Loaded)
                Refresh_Click(this, new RoutedEventArgs());
#endif
            return Task.CompletedTask;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => OnLoad(CurrentSettings);

        private void DevTool_Click(object sender, RoutedEventArgs e) => wb.ShowDevTools();

#if USE_BINDING
        internal sealed class LoadedBinding
        {
            public bool Loaded { set; get; } = false;


            public bool MarkAsLoaded() => Loaded = true;

            public void Unload() => Loaded = false;
        }
#endif
    }

    internal static class WebBrowserHelper
    {
        public static readonly DependencyProperty MenuHandlerProperty = DependencyProperty.RegisterAttached(nameof(MenuHandler), typeof(MenuHandler), typeof(WebBrowserHelper), new PropertyMetadata(OnMenuHandlerChanged));


        public static MenuHandler? GetMenuHandler(DependencyObject dependencyObject) => dependencyObject.GetValue(MenuHandlerProperty) as MenuHandler;

        public static void SetMenuHandler(DependencyObject dependencyObject, MenuHandler body) => dependencyObject.SetValue(MenuHandlerProperty, body);

        private static void OnMenuHandlerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChromiumWebBrowser browser)
                browser.MenuHandler = e.NewValue as MenuHandler;
        }
    }

    internal sealed class MenuHandler
        : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model) => model.Clear();

        public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags) => false;

        public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
        }

        public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback) => false;
    }

    internal sealed class MainWindowViewModel
        : INotifyPropertyChanged
    {
        private MenuHandler _menuHandler;

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };


        public MainWindowViewModel() => _menuHandler = new MenuHandler();

        public MenuHandler MenuHandler
        {
            get => _menuHandler;
            set
            {
                if (_menuHandler == value)
                    return;

                _menuHandler = value;
                RaisePropertyChanged(nameof(MenuHandler));
            }
        }


        public void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class StocksSettings
    {
        public string Symbol { set; get; } = "CURRENCYCOM:DE30";
        public int Interval { set; get; } = 5;
        public int Height { set; get; } = 250;
        public StocksStyle Style { set; get; } = StocksStyle.candles;
    }

    public enum StocksStyle
        : int
    {
        bars = 0,
        candles = 1,
        line = 2,
        aera = 3,
        kagi = 5,
        line_break = 7,
        hollow = 9,
    }
}
