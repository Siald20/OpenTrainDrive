using System;
using System.Collections.Generic;
using OTD.logic.svgAPI;

namespace OTD.logic.levelcrossing
{
    /// <summary>
    /// Zustande fur Bahnubergange.
    /// </summary>
    public enum LevelcrossingAspect
    {
    Open,
    Closed,
    Closure,
    }
    /// <summary>
    /// Logik und SVG-Abbildung fur einen Bahnubergang.
    /// </summary>
    public class Levelcrossing
    {
        public bool PowerOn { get; set; }
        public bool Error { get; set; }
        public bool ManualClose { get; set; }
        public bool AutomaticClose { get; set; }
        public bool Closure { get; set; }
        public string LevelcrossingName { get; set; } = "unnamed";
        public string SvgDirectory { get; set; } = "SVG";
        public string ColorOverride { get; set; }
        public string ClosureSvgFile { get; set; }
        public Dictionary<LevelcrossingAspect, string> SvgFileOverrides { get; } = new();
        public Dictionary<string, string> SvgColorOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SvgApi Svg { get; } = new SvgApi();
        public string SvgToken => Svg.Token;
        public string SvgFile => Svg.File;
        public string SvgPath => Svg.Path;

        public LevelcrossingAspect Aspect { get; private set; } = LevelcrossingAspect.Open;
        public bool FlashingLights { get; private set; }

        /// <summary>
        /// Aktualisiert den Zustand des Bahnubergangs anhand der Eingange.
        /// </summary>
        public void UpdateLevelcrossing()
        {
            FlashingLights = false;

            if (ManualClose)
            {
                FlashingLights = true;
                Aspect = LevelcrossingAspect.Closed;
                Console.WriteLine(LevelcrossingName + ": Manual close activated.");
            }
            else if (AutomaticClose)
            {
                FlashingLights = true;
                Aspect = LevelcrossingAspect.Closed;
                Console.WriteLine(LevelcrossingName + ": Automatic close activated.");
            }
            else
            {
                Aspect = LevelcrossingAspect.Open;
            }

            if (Closure)
            {
                Aspect = LevelcrossingAspect.Closure;
            }

            UpdateSvg();
        }

        /// <summary>
        /// Berechnet die SVG-Referenzen entsprechend dem aktuellen Zustand.
        /// </summary>
        public void UpdateSvg()
        {
            SvgUpdate.ForLevelcrossing(
                Svg,
                Aspect,
                SvgDirectory,
                ColorOverride,
                SvgFileOverrides,
                SvgColorOverrides,
                ClosureSvgFile);
        }

    }
}
