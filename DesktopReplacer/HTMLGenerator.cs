using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using Microsoft.WindowsAPICodePack.Shell;

using Unknown6656.Common;
using Unknown6656.IO;

namespace DesktopReplacer
{
    public sealed record GenerationResult(string HTML, string OutPath);

    public sealed class HTMLGenerator
    {
        private const string TEMP_PREFIX = "--temp-processed--";
        private static readonly Regex REPLACEMENT_REGEX = new(@"§(?<varname>\w+)§", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Dictionary<string, string> _source_cache = new();
        private readonly object _mutex = new();


        public string ReadTemplate(string file)
        {
            if (_source_cache.TryGetValue(file, out string? source))
                return source;

            string path = Path.Combine(DesktopReplacerWindow.RESOURCE_DIR.FullName, file);

            return _source_cache[file] = DataStream.FromFile(path).ToString(Encoding.Default).TrimStart('\xfeff');
        }

        public GenerationResult ModifyTemplate(string file, Dictionary<string, object?> replacements, bool write_back = false)
        {
            string source = ReadTemplate(file);
            string replaced = REPLACEMENT_REGEX.Replace(source, match => replacements.TryGetValue(match.Groups["varname"].Value, out object? value) ? value?.ToString() ?? "" : match.Value);
            string out_path = Path.Combine(DesktopReplacerWindow.USER_DATA_DIR.FullName, TEMP_PREFIX + file);

            if (write_back)
                DataStream.FromString(replaced, Encoding.Default).ToFile(out_path);

            return new(replaced, out_path.Replace('\\', '/'));
        }

        public void ResetTemplateCache() => _source_cache.Clear();


        public GenerationResult GenerateDesktopHTMLCode(Rectangle window_bounds, MonitorInfo[] monitors, string img_path, (RawIconInfo[], double, ViewMode)? icons) =>
            ModifyTemplate("desktop-template.html", new(StringComparer.InvariantCultureIgnoreCase)
            {
                ["style"] = GenerateDesktopCSSStyle(img_path).HTML,
                ["width"] = window_bounds.Width,
                ["height"] = window_bounds.Height,
                ["monitors"] = monitors.Select(m => GenerateMonitorHTMLCode(m, window_bounds.Top, window_bounds.Left, icons).HTML).StringJoinLines(),
                ["script"] = GenerateJSCode().HTML,
            }, write_back: true);

        public GenerationResult GenerateJSCode() =>
            ModifyTemplate("script-template.js", new(StringComparer.InvariantCultureIgnoreCase)
            {
                // TODO
            });

        public GenerationResult GenerateDesktopCSSStyle(string img_path) =>
            ModifyTemplate("style-template.css", new(StringComparer.InvariantCultureIgnoreCase)
            {
                ["background"] = img_path.Replace('\\', '/'),
                // TODO
            });

        public GenerationResult GenerateMonitorHTMLCode(MonitorInfo monitor, int window_top, int window_left, (RawIconInfo[], double, ViewMode)? icons)
        {
            StringBuilder sb_icons = new();

            if (monitor.IsPrimary /* TODO : fix this */ && icons is (RawIconInfo[] rawicons, double icon_size, ViewMode view_mode))
                _ = Parallel.ForEach(rawicons, icon =>
                {
                    double width = icon_size + 60;
                    double height = icon_size + 50;
                    string? icon_source = null;
                    bool hidden = false;

                    if (icon.MatchingFiles.Length > 0 && icon.MatchingFiles[0] is { } file)
                        try
                        {
                            hidden = file.Attributes.HasFlag(FileAttributes.Hidden);

                            using ShellFile shell_file = ShellFile.FromFilePath(file.FullName);

                            icon_source = DataStream.FromBitmap(shell_file.Thumbnail.Bitmap, ImageFormat.Png).ToDataURI("image/png");
                        }
                        catch
                        {
                        }

                    string html = ModifyTemplate("icon-template.html", new(StringComparer.InvariantCultureIgnoreCase)
                    {
                        ["name"] = icon.DisplayName,
                        ["files"] = $"[{icon.MatchingFiles.Select(f => $"'{f.FullName}'").StringJoin(",")}]",
                        ["width"] = width,
                        ["height"] = height,
                        ["left"] = icon.X * width,
                        ["top"] = icon.Y * height,
                        ["image"] = icon_source,
                        ["icon_size"] = icon_size,

                        // TODO
                    }).HTML;

                    lock (_mutex)
                        _ = sb_icons.AppendLine(html);
                });

            return ModifyTemplate("monitor-template.html", new(StringComparer.InvariantCultureIgnoreCase)
            {
                ["position_top"] = monitor.Top - window_top,
                ["position_left"] = monitor.Left - window_left,
                ["top"] = monitor.Top,
                ["left"] = monitor.Left,
                ["right"] = monitor.Right,
                ["bottom"] = monitor.Bottom,
                ["width"] = monitor.Width,
                ["height"] = monitor.Height,
                ["name"] = monitor.Name,
                ["primary"] = monitor.IsPrimary ? "(primary)" : "",
                ["frequency"] = monitor.Frequency,
                ["icons"] = sb_icons,
            });
        }
    }
}
