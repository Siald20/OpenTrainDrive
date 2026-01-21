using System;
using System.Collections.Generic;
using System.IO;
using OTD.logic.svgAPI;

namespace OTD.logic.signals
{
    /// <summary>
    /// Aspekte fuer das Signalsystem L.
    /// </summary>
    public enum SignalAspect_l
    {
        Magenta,
        H,
        NH,
        F1,
        F2,
        F2_BES,
        F3,
        F5, 
        F6,
        HIS,
        W,
        F1S,
        F2S,
        F3S,
        F5S,
        Dark,


    }

    /// <summary>
    /// Logik fuer ein Signalsystem L mit SVG-Ansteuerung.
    /// </summary>
    public class image_Signal_l()
    {

        public bool PowerOn { get; set; } = true;
        public bool Error { get; set; }
        public bool ManualStop { get; set; }
        public bool RouteSet { get; set; }
        public bool OccupiedEntrance { get; set; }
        public bool ShortEntrance { get; set; }
        public bool AppDark { get; set; }
        public bool His { get; set; }
        public bool SwitchClosure { get; set; }
        public int VelocityLimit { get; set; } = 0;
        public string SignalName { get; set; } = "unnamed";
        public string SvgDirectory { get; set; } = "SVG";
        public string ColorOverride { get; set; }
        public Dictionary<SignalAspect_l, string> SvgFileOverrides { get; } = new();
        public Dictionary<string, string> SvgColorOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SvgApi Svg { get; } = new SvgApi();
        public string SvgToken { get; private set; }
        public string SvgFile { get; private set; }
        public string SvgPath { get; private set; }


        public SignalAspect_l Aspect { get; private set; } = SignalAspect_l.Magenta;
        public bool MainLampRed { get; private set; }
        public bool MainLampEmergencyRed { get; private set; }
        public bool MainLampOrange1 { get; private set; }
        public bool MainLampOrange2 { get; private set; }
        public bool MainLampGreen1 { get; private set; }
        public bool MainLampGreen2 { get; private set; }
        public bool MainLampGreen3 { get; private set; }
        public bool AppLampOrange1 { get; private set; }
        public bool AppLampOrange2 { get; private set; }
        public bool AppLampOrange3 { get; private set; }
        public bool AppLampGreen1 { get; private set; }
        public bool AppLampGreen2 { get; private set; }
        public bool LampHis { get; private set; }

        /// <summary>
        /// Aktualisiert das Hauptsignal im System L.
        /// </summary>
        public void MainSignalSystemL()
        {
            MainLampRed = false;
            MainLampOrange1 = false;
            MainLampOrange2 = false;
            MainLampGreen1 = false;
            MainLampGreen2 = false;
            MainLampGreen3 = false;

            var canBeeGreen = PowerOn && !Error && RouteSet && SwitchClosure;
            
            if (!PowerOn || Error)
            {
                Aspect = SignalAspect_l.Magenta;
                Console.WriteLine($"{SignalName} Aspect set to Magenta because PowerOff or Error");
                UpdateSvg();
                return;
            }
            else if (PowerOn && !RouteSet)
            {
                Aspect = SignalAspect_l.H;
                Console.WriteLine($"{SignalName} Aspect set to H because ManualStop or Route not set");
                MainLampRed = true;
                UpdateSvg();
                return;
            }
            else if (PowerOn && Error)
            {
                Aspect = SignalAspect_l.NH;
                Console.WriteLine($"{SignalName} Aspect set to NH because Error");
                MainLampEmergencyRed = true;
                UpdateSvg();
                return;
            }
            else if (canBeeGreen && VelocityLimit == 0)
            {
                Aspect = SignalAspect_l.F1;
                Console.WriteLine($"{SignalName} Aspect set to F1 because can be green and VelocityLimit 0");
                MainLampGreen1 = true;
                UpdateSvg();
                return;
            }
            else if (canBeeGreen && VelocityLimit == 40)
            {
                Aspect = SignalAspect_l.F2;
                Console.WriteLine($"{SignalName} Aspect set to F2 because can be green and VelocityLimit 40");
                MainLampGreen1 = true;
                MainLampOrange2 = true;
                UpdateSvg();
                return;
            }
            else if (canBeeGreen && VelocityLimit == 60)
            {
                Aspect = SignalAspect_l.F3;
                Console.WriteLine($"{SignalName} Aspect set to F3 because can be green and VelocityLimit 60");
                MainLampGreen1 = true;
                MainLampGreen2 = true;
                UpdateSvg();
                return;
            }
            else if (canBeeGreen && VelocityLimit == 90)
            {
                Aspect = SignalAspect_l.F5;
                Console.WriteLine($"{SignalName} Aspect set to F5 because can be green and VelocityLimit 90");
                MainLampGreen1 = true;
                MainLampGreen2 = true;
                MainLampGreen3 = true;
                UpdateSvg();
                return;
            }
            else if (canBeeGreen && ShortEntrance)
            {
                Aspect = SignalAspect_l.F6;
                Console.WriteLine($"{SignalName} Aspect set to F6 because can be green and ShortEntrance");
                MainLampOrange1 = true;
                MainLampOrange2 = true;
                UpdateSvg();
                return;
            }
            else if (canBeeGreen && OccupiedEntrance)
            {
                Aspect = SignalAspect_l.F2_BES;
                Console.WriteLine($"{SignalName} Aspect set to F2_BES because can be green and OccupiedEntrance");
                MainLampGreen1 = true;
                MainLampOrange2 = true;
                UpdateSvg();
                return;
            }
            UpdateSvg();
        }
        /// <summary>
        /// Aktualisiert die HIS-Lampe im System L.
        /// </summary>
        public void his_signal_l()
        {
            LampHis = false;
        
            if (His)
            {
                Aspect = SignalAspect_l.HIS;
                Console.WriteLine($"{SignalName} Aspect set to HIS because His is true");
                LampHis = true;
            }
            UpdateSvg();
        }
        /// <summary>
        /// Aktualisiert das Vorsignal im System L.
        /// </summary>
        public void appSignal_l()
        {
            AppLampOrange1 = false;
            AppLampOrange2 = false;
            AppLampOrange3 = false;
            AppLampGreen1 = false;
            AppLampGreen2 = false;
        
            if (!PowerOn || Error)
            {
                Aspect = SignalAspect_l.Magenta;
            }
            else if (PowerOn && !RouteSet)
            {
                Aspect = SignalAspect_l.W;
                AppLampOrange1 = true;
            }
            else if (PowerOn && RouteSet && VelocityLimit == 0)
            {
                Aspect = SignalAspect_l.F1S;
                AppLampGreen1 = true;
                AppLampGreen2 = true;
            }
            else if (PowerOn && RouteSet && VelocityLimit == 40)
            {
                Aspect = SignalAspect_l.F2S;
                AppLampGreen1 = true;
                AppLampOrange1 = true;
            }
            else if (PowerOn && RouteSet && VelocityLimit == 60)
            {
                Aspect = SignalAspect_l.F3S;
                AppLampGreen1 = true;
                AppLampGreen2 = true;
                AppLampOrange1 = true;
            }
            else if (PowerOn && RouteSet && VelocityLimit == 90)
            {
                Aspect = SignalAspect_l.F5S;
                AppLampGreen1 = true;
                AppLampGreen2 = true;
                AppLampOrange3 = true;
            }
            else if (PowerOn && RouteSet && AppDark)
            {
                Aspect = SignalAspect_l.Dark;
            }
            
            UpdateSvg();
        }
        /// <summary>
        /// Berechnet die SVG-Referenzen fuer das aktuelle Signalbild.
        /// </summary>
        public void UpdateSvg()
        {
            var color = ColorOverride ?? MapAspectToColor();
            if (SvgFileOverrides.TryGetValue(Aspect, out var overrideFile) &&
                !string.IsNullOrWhiteSpace(overrideFile))
            {
                ApplySvgOverride(overrideFile, $"SVG.signal.aspect.{Aspect.ToString().ToLowerInvariant()}");
                return;
            }

            if (SvgColorOverrides.TryGetValue(color, out var colorOverride) &&
                !string.IsNullOrWhiteSpace(colorOverride))
            {
                ApplySvgOverride(colorOverride, $"SVG.signal.color.{color.ToLowerInvariant()}");
                return;
            }

            SvgToken = null;
            SvgFile = null;
            SvgPath = null;
            Svg.Replace(null);
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

        private string MapAspectToColor()
        {
            return Aspect switch
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
    }
}
