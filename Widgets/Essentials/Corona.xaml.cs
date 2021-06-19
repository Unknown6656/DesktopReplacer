using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Net;

namespace DesktopReplacer.Widgets.Essentials
{
    [WidgetInfo(nameof(Corona), "1.0")]
    public partial class Corona
    {
        public override CoronaSettings DefaultSettings => new()
        {
            Countries = new Dictionary<string, long>
            {
                ["DE"] = 83_783_942,
                ["FR"] = 65_273_511,
                ["UK"] = 67_886_011,
                ["IT"] = 60_461_826,
                ["ES"] = 46_754_778,
                ["RU"] = 145_934_462,
                ["CN"] = 1_439_323_776,
                ["US"] = 331_002_651,
                ["Σ"] = 7_781_980_002,
            }
        };


        public Corona() => InitializeComponent();

        public override void OnLoad(CoronaSettings settings)
        {
        }

        public override void OnUnload(ref CoronaSettings settings)
        {
        }

        public override async Task OnTick()
        {
            try
            {
                static string print(long num) => num.ToString("## ### ### ###").Trim();

                using WebClient wc = new();
                string json = await wc.DownloadStringTaskAsync("https://coronavirus-tracker-api.herokuapp.com/v2/locations");
                _response data = JsonSerializer.Deserialize<_response>(json);

                datagrid.ItemsSource = (from c in (from c in CurrentSettings.Countries.Keys
                                                   from l in data.locations
                                                   where l.country_code == c
                                                   group l.latest by l.country_code into g
                                                   select new _country
                                                   {
                                                       country_code = g.Key,
                                                       latest = new _latest
                                                       {
                                                           deaths = g.Sum(l => l.deaths),
                                                           confirmed = g.Sum(l => l.confirmed),
                                                           recovered = g.Sum(l => l.recovered),
                                                       },
                                                   }).Append(new _country
                                                   {
                                                       country_code = "Σ",
                                                       latest = data.latest
                                                   })
                                        let deaths = c.latest.deaths
                                        let cases = c.latest.confirmed
                                        let pop = CurrentSettings.Countries[c.country_code]
                                        select new
                                        {
                                            Country = c.country_code,
                                            Deaths = print(deaths),
                                            Cases = print(cases),
                                            KDR = $"{(double)deaths / cases * 100:F2}%",
                                            CPop = $"{(double)cases / pop * 1000:F4}‰",
                                            DPop = $"{(double)deaths / pop * 1000:F4}‰",
                                        } as object).Prepend(new
                                        {
                                            Country = "",
                                            Deaths = "Deaths",
                                            Cases = "Cases",
                                            KDR = "K/D-Ratio",
                                            CPop = "C/Pop.",
                                            DPop = "D/Pop.",
                                        });

                TickInterval = 600_000; // 10min
            }
            catch
            {
                TickInterval = 10_000;
            }
        }


        public struct _latest
        {
            public long confirmed;
            public long deaths;
            public long recovered;
        }

        public struct _country
        {
            public string country_code;
            public _latest latest;
        }

        public struct _response
        {
            public _latest latest;
            public List<_country> locations;
        }
    }

    public sealed class CoronaSettings
    {
        public Dictionary<string, long> Countries { set; get; } = new Dictionary<string, long>();
    }
}
