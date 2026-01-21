using System;
using System.Collections.Generic;
using OTD.logic.svgAPI;

namespace OTD.logic.signals
{
    /// <summary>
    /// Aspekte fuer das Signalsystem N.
    /// </summary>
    //muss noch an Signal System N angepasst werden
    public enum SignalAspect_n
    {
        Magenta,
        H,
        NH,
        M,
        Ges_4_an,
        Ges_4_ex,
        Ges_5_an,
        Ges_5_ex,
        Ges_6_an, 
        Ges_6_ex,
        Ges_7_an,
        Ges_7_ex,
        Ges_8_an,
        Ges_8_ex,
        Ges_9_an,
        Ges_9_ex,
        Ges_10_an,
        Ges_10_ex,
        Ges_11_an,
        Ges_11_ex,
        Ges_12_an,
        Ges_12_ex,
        Ges_13_an,
        Ges_13_ex,
        Ges_14_an,
        Ges_14_ex,
        Ges_15_an,
        Ges_15_ex,
        Ges_16_an,
        Ges_16_ex,
        Fes,
        
        


    }

    /// <summary>
    /// Logik fuer ein Signalsystem N mit SVG-Ansteuerung.
    /// </summary>
    public class image_Signal_n
    {
        public bool PowerOn { get; set; } = true;
        public bool Error { get; set; }
        public bool ManualStop { get; set; }
        public bool RouteSet { get; set; }
        public bool OccupiedEntrance { get; set; }
        public bool ShortEntrance { get; set; }
        public bool SwitchClosure { get; set; }
        public int VelocityLimit { get; set; } = 0;
        public string SignalName { get; set; } = "Unnamed";
        public string SvgDirectory { get; set; } = "SVG";
        public string ColorOverride { get; set; }
        public Dictionary<SignalAspect_n, string> SvgFileOverrides { get; } = new();
        public Dictionary<string, string> SvgColorOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SvgApi Svg { get; } = new SvgApi();
        public string SvgToken => Svg.Token;
        public string SvgFile => Svg.File;
        public string SvgPath => Svg.Path;


        public SignalAspect_n Aspect { get; private set; } = SignalAspect_n.Magenta;
        public bool LampRed { get; private set; }
        public bool LampEmergencyRed { get; private set; }
        public bool LampOrange { get; private set; }
        public bool LampGreen { get; private set; }
        public bool LampGes1 { get; private set; }
        public bool LampGes2 { get; private set; }
        public bool LampGes3 { get; private set; }

        /// <summary>
        /// Aktualisiert das Signalbild im System N.
        /// </summary>
        public void SignalSystemN()
        {
            LampRed = false;
            LampOrange = false;
            LampGreen = false;
            var canBeeGreen = PowerOn && !Error && RouteSet && SwitchClosure;

            if (!PowerOn || Error)
            {
                Aspect = SignalAspect_n.Magenta;
                Console.WriteLine($"{SignalName} Aspect set to Magenta because PowerOff or Error");
                UpdateSvg();
                return;
            }

            if (PowerOn && (ManualStop || !RouteSet))
            {
                Aspect = SignalAspect_n.H;
                Console.WriteLine($"{SignalName} Aspect set to H because ManualStop or Route not set");
                LampRed = true;
                UpdateSvg();
                return;
            }

            if (PowerOn && Error)
            {
                Aspect = SignalAspect_n.NH;
                Console.WriteLine($"{SignalName} Aspect set to NH because Error");
                LampEmergencyRed = true;
                UpdateSvg();
                return;
            }

            if (canBeeGreen && VelocityLimit == 0)
            {
                Aspect = SignalAspect_n.M;
                Console.WriteLine($"{SignalName} Aspect set to M because can be green and VelocityLimit 0");
                LampGreen = true;
                UpdateSvg();
                return;
            }
            if (canBeeGreen && VelocityLimit == 40)
            {
                Aspect = SignalAspect_n.Ges_4_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_4_ex because can be green and VelocityLimit 40");
                LampGreen = true;
                UpdateSvg();
                return;
            }
            if (canBeeGreen && VelocityLimit == 50)
            {
                Aspect = SignalAspect_n.Ges_5_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_5_ex because can be green and VelocityLimit 50");
                LampGreen = true;
                UpdateSvg();
                return;
            }
            if (canBeeGreen && VelocityLimit == 60)
            {
                Aspect = SignalAspect_n.Ges_6_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_6_ex because can be green and VelocityLimit 60");
                LampGreen = true;
                UpdateSvg();
                return;
            }
            if (canBeeGreen && VelocityLimit == 70)
            {
                Aspect = SignalAspect_n.Ges_7_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_7_ex because can be green and VelocityLimit 70");
                LampGreen = true;
                UpdateSvg();
                
                return;
            }
            if (canBeeGreen && VelocityLimit == 80)
            {
                Aspect = SignalAspect_n.Ges_8_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_8_ex because can be green and VelocityLimit 80");
                LampGreen = true;
                UpdateSvg();
                
                return;
            }
                if (canBeeGreen && VelocityLimit == 90)
            {
                Aspect = SignalAspect_n.Ges_9_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_9_ex because can be green and VelocityLimit 90");
                LampGreen = true;
                UpdateSvg();
                
                return;
            }
                if (canBeeGreen && VelocityLimit == 100)
            {
                Aspect = SignalAspect_n.Ges_10_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_10_ex because can be green and VelocityLimit 100");
                LampGreen = true;
                UpdateSvg();
                
                return;
            }

            if (canBeeGreen && VelocityLimit == 110)
            {
                Aspect = SignalAspect_n.Ges_11_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_11_ex because can be green and VelocityLimit 110");
                LampGreen = true;
                UpdateSvg();
                
                return;
            }
            if (canBeeGreen && VelocityLimit == 120)
            {
                Aspect = SignalAspect_n.Ges_12_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_12_ex because can be green and VelocityLimit 120");
                LampGreen = true;
                UpdateSvg();
                
                return;
            }
                if (canBeeGreen && VelocityLimit == 130)
            {
                Aspect = SignalAspect_n.Ges_13_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_13_ex because can be green and VelocityLimit 130");
                LampGreen = true;
                UpdateSvg();
                
                return;
            }
                if (canBeeGreen && VelocityLimit == 140)
            {
                Aspect = SignalAspect_n.Ges_14_ex;
                Console.WriteLine($"{SignalName} Aspect set to Ges_14_ex because can be green and VelocityLimit 140");
                LampGreen = true;
                UpdateSvg();
                
                return;
            }

                if (canBeeGreen && VelocityLimit == 150)
            {
                    Aspect = SignalAspect_n.Ges_15_ex;
                    Console.WriteLine($"{SignalName} Aspect set to Ges_15_ex because can be green and VelocityLimit 150");
                    LampGreen = true;
                    UpdateSvg();
                    
                    return;
            }
                if (canBeeGreen && VelocityLimit == 160)
            {
                    Aspect = SignalAspect_n.Ges_16_ex;
                    Console.WriteLine($"{SignalName} Aspect set to Ges_16_ex because can be green and VelocityLimit 160");
                    LampGreen = true;
                    UpdateSvg();
                    
                    return;
            }
            
                else
            {
                
            }
        
        }

        /// <summary>
        /// Berechnet die SVG-Referenzen fuer das aktuelle Signalbild.
        /// </summary>
        public void UpdateSvg()
        {
            SvgUpdate.ForSignalN(
                Svg,
                Aspect,
                SvgDirectory,
                ColorOverride,
                SvgFileOverrides,
                SvgColorOverrides);
        }

    }
}
