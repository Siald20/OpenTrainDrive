using System;
using System.Collections.Generic;
using System.IO;
using OTD.logic.levelcrossing;
using OTD.logic.signals;
using OTD.logic.switches;

namespace OTD.logic.svgAPI
{
    /// <summary>
    /// Verwaltet Basis- und Zusatzlayer fuer SVG-Darstellungen.
    /// </summary>
    public sealed class SvgApi
    {
        private readonly List<SvgLayer> _layers = new();

        public SvgLayer? BaseLayer { get; private set; }
        public string? BaseSvg => BaseLayer?.File;
        public string? Token => BaseLayer?.Token;
        public string? File => BaseLayer?.File;
        public string? Path => BaseLayer?.Path;
        public IReadOnlyList<SvgLayer> Layers => _layers;

        /// <summary>
        /// Setzt die Basisschicht und leert die Zusatzlayer.
        /// </summary>
        public void Replace(string? svgPath, string? token = null, string? svgDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(svgPath))
            {
                BaseLayer = null;
                _layers.Clear();
                return;
            }

            BaseLayer = BuildLayer(svgPath, token, svgDirectory);
            _layers.Clear();
        }

        /// <summary>
        /// Fuegt einen zusaetzlichen Layer hinzu.
        /// </summary>
        public void Add(string svgPath, string? token = null, string? svgDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(svgPath))
            {
                return;
            }

            _layers.Add(BuildLayer(svgPath, token, svgDirectory));
        }

        /// <summary>
        /// Liefert alle SVG-Dateien der Basis- und Zusatzlayer.
        /// </summary>
        public IEnumerable<string> GetAll()
        {
            if (!string.IsNullOrWhiteSpace(BaseSvg))
            {
                yield return BaseSvg!;
            }

            foreach (var layer in _layers)
            {
                yield return layer.File;
            }
        }

        private static SvgLayer BuildLayer(string path, string? token, string? svgDirectory)
        {
            var normalized = Normalize(path, svgDirectory);
            return new SvgLayer(normalized, $"/svg/{normalized}", token);
        }

        private static string Normalize(string path, string? svgDirectory)
        {
            var normalized = path.Replace('\\', '/').Trim();
            if (System.IO.Path.IsPathRooted(path))
            {
                normalized = System.IO.Path.GetFileName(path);
            }
            while (normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(1);
            }
            if (normalized.StartsWith("svg/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(4);
            }
            if (!string.IsNullOrWhiteSpace(svgDirectory))
            {
                var svgDir = svgDirectory.Replace('\\', '/').Trim('/');
                if (!string.IsNullOrWhiteSpace(svgDir) &&
                    normalized.StartsWith(svgDir + "/", StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized.Substring(svgDir.Length + 1);
                }
            }
            return normalized;
        }
    }

    /// <summary>
    /// Datenobjekt fuer einen einzelnen SVG-Layer.
    /// </summary>
    public sealed class SvgLayer
    {
        public SvgLayer(string file, string path, string? token)
        {
            File = file;
            Path = path;
            Token = token;
        }

        public string File { get; }
        public string Path { get; }
        public string? Token { get; }
    }

    /// <summary>
    /// Erzeugt SVG-Updates fuer Weichen, Signale und Bahnuebergaenge.
    /// </summary>
    public static class SvgUpdate
    {
        /// <summary>
        /// Aktualisiert die SVG-Referenz fuer eine Weiche.
        /// </summary>
        public static void ForSwitch(
            SvgApi svg,
            SingleSwitchAspect aspect,
            bool switchLeft,
            string svgDirectory,
            string? colorOverride,
            IDictionary<SingleSwitchAspect, string> aspectOverrides,
            IDictionary<string, string> colorOverrides,
            string? closureSvgFile)
        {
            var color = colorOverride ?? MapSwitchAspectToColor(aspect);
            var baseElementName = switchLeft ? "Switch_Turnout" : "Switch_Straight";
            string? svgPath = null;
            string? token = null;

            if (aspectOverrides.TryGetValue(aspect, out var overrideFile) &&
                !string.IsNullOrWhiteSpace(overrideFile))
            {
                svgPath = overrideFile;
                token = $"SVG.switch.aspect.{aspect.ToString().ToLowerInvariant()}";
            }
            else if (IsSwitchClosureAspect(aspect))
            {
                if (!string.IsNullOrWhiteSpace(closureSvgFile))
                {
                    svgPath = closureSvgFile;
                    token = $"SVG.switch.closure.{color.ToLowerInvariant()}";
                }
                else
                {
                    var closureElementName = $"{baseElementName}_Closure";
                    var closureFile = SvgSymbolResolver.ResolveSvgFile(closureElementName, color, svgDirectory);
                    if (closureFile != null)
                    {
                        svgPath = closureFile;
                        token = $"SVG.{closureElementName.ToLowerInvariant()}.{color.ToLowerInvariant()}";
                    }
                }
            }

            if (svgPath == null &&
                colorOverrides.TryGetValue(color, out var colorOverrideFile) &&
                !string.IsNullOrWhiteSpace(colorOverrideFile))
            {
                svgPath = colorOverrideFile;
                token = $"SVG.switch.color.{color.ToLowerInvariant()}";
            }

            if (svgPath == null)
            {
                var resolved = SvgSymbolResolver.ResolveSvgFile(baseElementName, color, svgDirectory);
                svgPath = resolved;
                token = resolved == null ? null : $"SVG.{baseElementName.ToLowerInvariant()}.{color.ToLowerInvariant()}";
            }

            svg.Replace(svgPath, token, svgDirectory);
        }

        /// <summary>
        /// Aktualisiert die SVG-Referenz fuer ein Signal (System L).
        /// </summary>
        public static void ForSignalL(
            SvgApi svg,
            SignalAspect_l aspect,
            string svgDirectory,
            string? colorOverride,
            IDictionary<SignalAspect_l, string> aspectOverrides,
            IDictionary<string, string> colorOverrides)
        {
            var color = colorOverride ?? MapSignalLAspectToColor(aspect);
            string? svgPath = null;
            string? token = null;

            if (aspectOverrides.TryGetValue(aspect, out var overrideFile) &&
                !string.IsNullOrWhiteSpace(overrideFile))
            {
                svgPath = overrideFile;
                token = $"SVG.signal.aspect.{aspect.ToString().ToLowerInvariant()}";
            }
            else if (colorOverrides.TryGetValue(color, out var colorOverrideFile) &&
                     !string.IsNullOrWhiteSpace(colorOverrideFile))
            {
                svgPath = colorOverrideFile;
                token = $"SVG.signal.color.{color.ToLowerInvariant()}";
            }

            svg.Replace(svgPath, token, svgDirectory);
        }

        /// <summary>
        /// Aktualisiert die SVG-Referenz fuer ein Signal (System N).
        /// </summary>
        public static void ForSignalN(
            SvgApi svg,
            SignalAspect_n aspect,
            string svgDirectory,
            string? colorOverride,
            IDictionary<SignalAspect_n, string> aspectOverrides,
            IDictionary<string, string> colorOverrides)
        {
            var color = colorOverride ?? MapSignalNAspectToColor(aspect);
            string? svgPath = null;
            string? token = null;

            if (aspectOverrides.TryGetValue(aspect, out var overrideFile) &&
                !string.IsNullOrWhiteSpace(overrideFile))
            {
                svgPath = overrideFile;
                token = $"SVG.signaln.aspect.{aspect.ToString().ToLowerInvariant()}";
            }
            else if (colorOverrides.TryGetValue(color, out var colorOverrideFile) &&
                     !string.IsNullOrWhiteSpace(colorOverrideFile))
            {
                svgPath = colorOverrideFile;
                token = $"SVG.signaln.color.{color.ToLowerInvariant()}";
            }

            svg.Replace(svgPath, token, svgDirectory);
        }

        /// <summary>
        /// Aktualisiert die SVG-Referenz fuer einen Bahnuebergang.
        /// </summary>
        public static void ForLevelcrossing(
            SvgApi svg,
            LevelcrossingAspect aspect,
            string svgDirectory,
            string? colorOverride,
            IDictionary<LevelcrossingAspect, string> aspectOverrides,
            IDictionary<string, string> colorOverrides,
            string? closureSvgFile)
        {
            var color = colorOverride ?? MapLevelcrossingAspectToColor(aspect);
            string? svgPath = null;
            string? token = null;

            if (aspectOverrides.TryGetValue(aspect, out var overrideFile) &&
                !string.IsNullOrWhiteSpace(overrideFile))
            {
                svgPath = overrideFile;
                token = $"SVG.levelcrossing.aspect.{aspect.ToString().ToLowerInvariant()}";
            }
            else if (aspect == LevelcrossingAspect.Closure &&
                     !string.IsNullOrWhiteSpace(closureSvgFile))
            {
                svgPath = closureSvgFile;
                token = $"SVG.levelcrossing.closure.{color.ToLowerInvariant()}";
            }
            else if (colorOverrides.TryGetValue(color, out var colorOverrideFile) &&
                     !string.IsNullOrWhiteSpace(colorOverrideFile))
            {
                svgPath = colorOverrideFile;
                token = $"SVG.levelcrossing.color.{color.ToLowerInvariant()}";
            }

            svg.Replace(svgPath, token, svgDirectory);
        }

        private static bool IsSwitchClosureAspect(SingleSwitchAspect aspect)
        {
            return aspect == SingleSwitchAspect.Closure ||
                   aspect == SingleSwitchAspect.SingleClosure ||
                   aspect == SingleSwitchAspect.SideCollisonClosure ||
                   aspect == SingleSwitchAspect.SwitchCut;
        }

        private static string MapSwitchAspectToColor(SingleSwitchAspect aspect)
        {
            return aspect switch
            {
                SingleSwitchAspect.left => "Green",
                SingleSwitchAspect.right => "Green",
                SingleSwitchAspect.Closure => "Green",
                SingleSwitchAspect.SingleClosure => "Green",
                SingleSwitchAspect.SideCollisonClosure => "Green",
                SingleSwitchAspect.SwitchCut => "Green",
                SingleSwitchAspect.magenta => "White",
                _ => "White"
            };
        }

        private static string MapSignalLAspectToColor(SignalAspect_l aspect)
        {
            return aspect switch
            {
                SignalAspect_l.Magenta => "Magenta",
                SignalAspect_l.HIS => "Red",
                SignalAspect_l.H => "Red",
                SignalAspect_l.NH => "Red",
                SignalAspect_l.F1 => "Green",
                SignalAspect_l.F2 => "Green",
                SignalAspect_l.F2_BES => "Green",
                SignalAspect_l.F3 => "Green",
                SignalAspect_l.F5 => "Green",
                SignalAspect_l.F6 => "Green",
                _ => "Magenta"
            };
        }

        private static string MapSignalNAspectToColor(SignalAspect_n aspect)
        {
            return aspect switch
            {
                SignalAspect_n.Magenta => "Magenta",
                SignalAspect_n.H => "Red",
                SignalAspect_n.NH => "Red",
                SignalAspect_n.M => "Green",
                SignalAspect_n.Ges_4_an => "Green",
                SignalAspect_n.Ges_4_ex => "Green",
                SignalAspect_n.Ges_5_an => "Green",
                SignalAspect_n.Ges_5_ex => "Green",
                SignalAspect_n.Ges_6_an => "Green",
                SignalAspect_n.Ges_6_ex => "Green",
                SignalAspect_n.Ges_7_an => "Green",
                SignalAspect_n.Ges_7_ex => "Green",
                SignalAspect_n.Ges_8_an => "Green",
                SignalAspect_n.Ges_8_ex => "Green",
                SignalAspect_n.Ges_9_an => "Green",
                SignalAspect_n.Ges_9_ex => "Green",
                SignalAspect_n.Ges_10_an => "Green",
                SignalAspect_n.Ges_10_ex => "Green",
                SignalAspect_n.Ges_11_an => "Green",
                SignalAspect_n.Ges_11_ex => "Green",
                SignalAspect_n.Ges_12_an => "Green",
                SignalAspect_n.Ges_12_ex => "Green",
                SignalAspect_n.Ges_13_an => "Green",
                SignalAspect_n.Ges_13_ex => "Green",
                SignalAspect_n.Ges_14_an => "Green",
                SignalAspect_n.Ges_14_ex => "Green",
                SignalAspect_n.Ges_15_an => "Green",
                SignalAspect_n.Ges_15_ex => "Green",
                SignalAspect_n.Ges_16_an => "Green",
                SignalAspect_n.Ges_16_ex => "Green",
                _ => "Green"
            };
        }

        private static string MapLevelcrossingAspectToColor(LevelcrossingAspect aspect)
        {
            return aspect switch
            {
                LevelcrossingAspect.Open => "Green",
                LevelcrossingAspect.Closed => "Red",
                LevelcrossingAspect.Closure => "Red",
                _ => "White"
            };
        }
    }
}
