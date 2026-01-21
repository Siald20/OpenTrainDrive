using System.Data;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using OTD.logic.svgAPI;

namespace OTD.logic.switches
{
    /// <summary>
    /// Zustandswerte fuer eine einfache Weiche.
    /// </summary>
    public enum SingleSwitchAspect
    {
    left,
    right,
    magenta,
    Closure,
    SingleClosure,
    SideCollisonClosure,
    SwitchCut


    }

    /// <summary>
    /// Steuert die Logik einer einzelnen Weiche inklusive SVG-Ausgabe.
    /// </summary>
    public class SingleSwitch
    {
        public bool PowerOn { get; set; }
        public bool Error { get; set; }
        public bool ManualSwitch { get; set; }
        public bool AutomaticSwitchRightWithClosure { get; set; }
        public bool AutomaticSwitchLeftWithClosure { get; set; }
        public bool SwitchClosure { get; set; }
        public bool SingleClosure { get; set; }
        public bool Occupied { get; set; }
        public bool SideCollisonClosure { get; set; }
        public bool SwitchCut { get; set; }
        public string SwitchName { get; set; } = "unnamed";
        public string SvgDirectory { get; set; } = "SVG";
        public string ColorOverride { get; set; }
        public string ClosureSvgFile { get; set; }
        public Dictionary<SingleSwitchAspect, string> SvgFileOverrides { get; } = new();
        public Dictionary<string, string> SvgColorOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SvgApi Svg { get; } = new SvgApi();
        public string SvgToken { get; private set; }
        public string SvgFile { get; private set; }
        public string SvgPath { get; private set; }

        public SingleSwitchAspect Aspect { get; private set; } = SingleSwitchAspect.magenta;
        public bool SwitchLeft { get; private set; }
        public bool SwitchRight { get; private set; } = true;

        private bool _lastManualSwitch;

        /// <summary>
        /// Aktualisiert den Weichenzustand anhand der Eingaben.
        /// </summary>
        public void UpdateSwitch()
        {
            var canSwitch = PowerOn && !Error && !SwitchClosure && !SingleClosure && !Occupied && !SideCollisonClosure && !SwitchCut;

            if (!SwitchLeft && !SwitchRight)
            {
                SwitchRight = true;
            }

        

            if (ManualSwitch && !_lastManualSwitch && canSwitch)
            {
                SwitchLeft = !SwitchLeft;
                SwitchRight = !SwitchLeft;
                Console.WriteLine(SwitchName + ": Manual switch toggled.");
            }

            _lastManualSwitch = ManualSwitch;
            if (AutomaticSwitchRightWithClosure && canSwitch)
            {
                SwitchLeft = false;
                SwitchRight = true;
                SwitchClosure = true;
                Console.WriteLine(SwitchName + ": Automatic switch to right with closure activated.");
                UpdateSvg();
                return;
            }
            if (AutomaticSwitchLeftWithClosure && canSwitch)
            {
                SwitchLeft = true;
                SwitchRight = false;
                SwitchClosure = true;
                Console.WriteLine(SwitchName + ": Automatic switch to left with closure activated.");
                UpdateSvg();
                return;
            }
            if (!PowerOn || Error)
            {
                Aspect = SingleSwitchAspect.magenta;
                Console.WriteLine(SwitchName + ": Aspect set to magenta because PowerOff or Error");
                UpdateSvg();
                return;
            }

            if (SwitchCut)
            {
                Aspect = SingleSwitchAspect.SwitchCut;
                Console.WriteLine(SwitchName + ": Aspect set to SwitchCut");
                UpdateSvg();
                return;
            }
            else if (SideCollisonClosure)
            {
                Aspect = SingleSwitchAspect.SideCollisonClosure;
                Console.WriteLine(SwitchName + ": Aspect set to SideCollisonClosure");
                UpdateSvg();
                return;
            }
            else if (SingleClosure)
            {
                Aspect = SingleSwitchAspect.SingleClosure;
                Console.WriteLine(SwitchName + ": Aspect set to SingleClosure");
                UpdateSvg();
                return;
            }
            else if (SwitchClosure)
            {
                Aspect = SingleSwitchAspect.Closure;
                Console.WriteLine(SwitchName + ": Aspect set to Closure");
                UpdateSvg();
                return;
            }
            else if (SwitchLeft)
            {
                SwitchLeft = true;
                SwitchRight = false;
                Aspect = SingleSwitchAspect.left;
                Console.WriteLine(SwitchName + ": Aspect set to left");
                UpdateSvg();
                return;
            }
            else if (SwitchRight)
            {
                SwitchLeft = false;
                SwitchRight = true;
                Aspect = SingleSwitchAspect.right;
                Console.WriteLine(SwitchName + ": Aspect set to right");
                UpdateSvg();
                return;
            }
            else
            {
                SwitchCut = true;
                Aspect = SingleSwitchAspect.SwitchCut;
                Console.WriteLine(SwitchName + ": Aspect set to SwitchCut as fallback");
            }

            UpdateSvg();
        }

        /// <summary>
        /// Aktualisiert die SVG-Referenzen entsprechend dem aktuellen Zustand.
        /// </summary>
        public void UpdateSvg()
        {
            var baseElementName = SwitchLeft ? "Switch_Turnout" : "Switch_Straight";
            var color = ColorOverride ?? MapAspectToColor();
            var elementName = baseElementName;

            if (SvgFileOverrides.TryGetValue(Aspect, out var overrideFile) &&
                !string.IsNullOrWhiteSpace(overrideFile))
            {
                ApplySvgOverride(overrideFile, $"SVG.switch.aspect.{Aspect.ToString().ToLowerInvariant()}");
                return;
            }

            if (IsClosureAspect(Aspect))
            {
                if (!string.IsNullOrWhiteSpace(ClosureSvgFile))
                {
                    ApplySvgOverride(ClosureSvgFile, $"SVG.switch.closure.{color.ToLowerInvariant()}");
                    return;
                }

                var closureElementName = $"{baseElementName}_Closure";
                var closureFile = SvgSymbolResolver.ResolveSvgFile(closureElementName, color, SvgDirectory);
                if (closureFile != null)
                {
                    elementName = closureElementName;
                    SvgToken = $"SVG.{elementName.ToLowerInvariant()}.{color.ToLowerInvariant()}";
                    SvgFile = closureFile;
                    SvgPath = $"/svg/{closureFile}";
                    Svg.Replace(closureFile);
                    return;
                }
            }

            if (SvgColorOverrides.TryGetValue(color, out var colorOverride) &&
                !string.IsNullOrWhiteSpace(colorOverride))
            {
                ApplySvgOverride(colorOverride, $"SVG.switch.color.{color.ToLowerInvariant()}");
                return;
            }

            SvgToken = $"SVG.{elementName.ToLowerInvariant()}.{color.ToLowerInvariant()}";
            SvgFile = SvgSymbolResolver.ResolveSvgFile(elementName, color, SvgDirectory);
            SvgPath = SvgSymbolResolver.ResolveSvgPath(elementName, color, SvgDirectory);
            Svg.Replace(SvgFile);
        }

        private string MapAspectToColor()
        {
            return Aspect switch
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

        private static bool IsClosureAspect(SingleSwitchAspect aspect)
        {
            return aspect == SingleSwitchAspect.Closure ||
                aspect == SingleSwitchAspect.SingleClosure ||
                aspect == SingleSwitchAspect.SideCollisonClosure ||
                aspect == SingleSwitchAspect.SwitchCut;
        }

        private void ApplySvgOverride(string svgOverride, string token)
        {
            if (string.IsNullOrWhiteSpace(svgOverride))
            {
                return;
            }

            var normalized = svgOverride.Replace('\\', '/').TrimStart('/');
            if (Path.IsPathRooted(svgOverride))
            {
                normalized = Path.GetFileName(svgOverride);
            }

            var svgDir = SvgDirectory.Replace('\\', '/').Trim('/');
            if (!string.IsNullOrWhiteSpace(svgDir) &&
                normalized.StartsWith(svgDir + "/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(svgDir.Length + 1);
            }

            SvgToken = token;
            SvgFile = normalized;
            SvgPath = $"/svg/{normalized}";
            Svg.Replace(normalized);
        }
    }
}
