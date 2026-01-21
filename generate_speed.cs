using System;
using System.Xml.Linq;

namespace OpenTrainDrive.Tools;

/* rein KI generierter Code, hat aber nicht zum richtigen Ergebnis gefuehrt: An sich war der Auftrag, den
 gesamten Geschwindigkeitbereich linear von 1 bis 255 auf die 128 Geschwindigkeitsstufen abzubilden. P.S.: das Auto-Ergaenzen
 hier im Code-Editor hat geschnallt was ich wollte, nachdem ich den halben Text geschrieben hatte ;-). */

/// <summary>
/// Hilfsklasse zum Erzeugen einer linearen Geschwindigkeitstabelle.
/// </summary>
class LocoSpeedtools
{
    /// <summary>
    /// Gibt eine 128-stufige Geschwindigkeitstabelle als XML aus.
    /// </summary>
    static void CalculatingSpeedTable()
    {
        // Generate a linear speed step table for 128 speed steps
        // Speed step 1 = stop (value 0)
        // Speed step 2-128 = linear from 1 to 255

        var speedtable = new XElement("speedtable");

        // For speed step 1: stop
        speedtable.Add(new XElement("step", new XAttribute("speed", 1), new XAttribute("value", 0)));

        // For speed steps 2 to 128: linear
        for (int step = 2; step <= 128; step++)
        {
            // Linear mapping: step 2 = 1, step 128 = 255
            int value = (int)Math.Round((step - 1) * 255.0 / 127.0);
            speedtable.Add(new XElement("step", new XAttribute("speed", step), new XAttribute("value", value)));
        }

        Console.WriteLine(speedtable.ToString());
    }
}


