using System.Text.RegularExpressions;

namespace OpenTrainDrive;

/// <summary>
/// Hilfsfunktionen zum Umschreiben von SVG-Dateinamen anhand von Logikfeldern.
/// </summary>
public static class FsLogic
{
    /// <summary>
    /// Wendet eine einzelne Feldregel auf einen SVG-Dateinamen an.
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <param name="fieldKey">Schluessel des Feldes.</param>
    /// <param name="fieldValue">Wert des Feldes.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ApplySvgField(string file, string? fieldKey, string? fieldValue)
    {
        if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(fieldKey))
        {
            return file;
        }

        var normalized = NormalizeSvgFile(file);
        var key = fieldKey.Trim().ToLowerInvariant();
        var value = (fieldValue ?? string.Empty).Trim().ToLowerInvariant();

        return key switch
        {
            "turnout" => value switch
            {
                "diverge" => ToSwitchTurnoutGreen(normalized),
                "straight" => ToSwitchStraightGreen(normalized),
                _ => normalized
            },
            "route" => value == "on" ? ToNumberGreen(ToTrackGreen(normalized)) : normalized,
            "signal" => value switch
            {
                "green" => ToSignalGreen(normalized),
                "red" => ToSignalRed(normalized),
                _ => normalized
            },
            _ => normalized
        };
    }

    /// <summary>
    /// Wendet mehrere Feldregeln nacheinander auf einen SVG-Dateinamen an.
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <param name="fields">Liste der Feldregeln.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ApplySvgFields(string file, IEnumerable<KeyValuePair<string, string?>> fields)
    {
        var current = file;
        foreach (var field in fields)
        {
            current = ApplySvgField(current, field.Key, field.Value);
        }
        return current;
    }

    /// <summary>
    /// Normalisiert bekannte Sonderfaelle von SVG-Dateinamen.
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der normalisierte Dateiname.</returns>
    public static string NormalizeSvgFile(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return file;
        }
        if (file.Contains("Iltis_Zugnummernanzeiger.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Zugnummernanzeiger.svg", "Iltis_Zugnummernanzeiger_White.svg", StringComparison.Ordinal);
        }
        return file;
    }

    /// <summary>
    /// Setzt eine Weiche auf Gerade (weiss).
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ToSwitchStraight(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return file;
        }
        if (file.Contains("Iltis_Switch_Straight.svg", StringComparison.Ordinal)) return file;
        if (file.Contains("Iltis_Switch_Straight_White.svg", StringComparison.Ordinal)) return file;
        if (file.Contains("Iltis_Switch_Straight_Green.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_Straight_Green.svg", "Iltis_Switch_Straight_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch_Turnout.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_Turnout.svg", "Iltis_Switch_Straight.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch_Turnout_White.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_Turnout_White.svg", "Iltis_Switch_Straight_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch_Turnout_Green.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_Turnout_Green.svg", "Iltis_Switch_Straight_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch_White.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_White.svg", "Iltis_Switch_Straight_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch.svg", "Iltis_Switch_Straight_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Turnout_Left.svg", StringComparison.Ordinal) ||
            file.Contains("Iltis_Turnout_Right.svg", StringComparison.Ordinal) ||
            file.Contains("Iltis_Turnout.svg", StringComparison.Ordinal))
        {
            return Regex.Replace(file, @"Iltis_Turnout_(Left|Right)\.svg|Iltis_Turnout\.svg", "Iltis_Switch_Straight.svg");
        }
        return file;
    }

    /// <summary>
    /// Setzt eine Weiche auf Abzweig (weiss).
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ToSwitchTurnout(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return file;
        }
        if (file.Contains("Iltis_Switch_Turnout.svg", StringComparison.Ordinal)) return file;
        if (file.Contains("Iltis_Switch_Turnout_White.svg", StringComparison.Ordinal)) return file;
        if (file.Contains("Iltis_Switch_Turnout_Green.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_Turnout_Green.svg", "Iltis_Switch_Turnout_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch_Straight.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_Straight.svg", "Iltis_Switch_Turnout.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch_Straight_White.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_Straight_White.svg", "Iltis_Switch_Turnout_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch_Straight_Green.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_Straight_Green.svg", "Iltis_Switch_Turnout_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch_White.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch_White.svg", "Iltis_Switch_Turnout_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Switch.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Switch.svg", "Iltis_Switch_Turnout_White.svg", StringComparison.Ordinal);
        }
        if (file.Contains("Iltis_Turnout_Left.svg", StringComparison.Ordinal) ||
            file.Contains("Iltis_Turnout_Right.svg", StringComparison.Ordinal) ||
            file.Contains("Iltis_Turnout.svg", StringComparison.Ordinal))
        {
            return Regex.Replace(file, @"Iltis_Turnout_(Left|Right)\.svg|Iltis_Turnout\.svg", "Iltis_Switch_Turnout.svg");
        }
        return file;
    }

    /// <summary>
    /// Setzt eine Weiche auf Gerade (gruen).
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ToSwitchStraightGreen(string file)
    {
        var white = ToSwitchStraight(file);
        if (string.IsNullOrWhiteSpace(white))
        {
            return white;
        }
        if (white.Contains("Iltis_Switch_Straight_White.svg", StringComparison.Ordinal))
        {
            return white.Replace("Iltis_Switch_Straight_White.svg", "Iltis_Switch_Straight_Green.svg", StringComparison.Ordinal);
        }
        return white.Replace("Iltis_Switch_Straight.svg", "Iltis_Switch_Straight_Green.svg", StringComparison.Ordinal);
    }

    /// <summary>
    /// Setzt eine Weiche auf Abzweig (gruen).
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ToSwitchTurnoutGreen(string file)
    {
        var white = ToSwitchTurnout(file);
        if (string.IsNullOrWhiteSpace(white))
        {
            return white;
        }
        if (white.Contains("Iltis_Switch_Turnout_White.svg", StringComparison.Ordinal))
        {
            return white.Replace("Iltis_Switch_Turnout_White.svg", "Iltis_Switch_Turnout_Green.svg", StringComparison.Ordinal);
        }
        return white.Replace("Iltis_Switch_Turnout.svg", "Iltis_Switch_Turnout_Green.svg", StringComparison.Ordinal);
    }

    /// <summary>
    /// Setzt eine Strecke auf gruen.
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ToTrackGreen(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return file;
        }
        if (file.Contains("Iltis_Straight_Green.svg", StringComparison.Ordinal)) return file;
        if (file.Contains("Iltis_Straight_White", StringComparison.Ordinal))
        {
            return Regex.Replace(file, @"Iltis_Straight_White[^/]*\.svg", "Iltis_Straight_Green.svg", RegexOptions.IgnoreCase);
        }
        return file;
    }

    /// <summary>
    /// Setzt den Zugnummernanzeiger auf gruen.
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ToNumberGreen(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return file;
        }
        if (file.Contains("Iltis_Zugnummernanzeiger_Green.svg", StringComparison.Ordinal)) return file;
        if (file.Contains("Iltis_Zugnummernanzeiger_White.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Zugnummernanzeiger_White.svg", "Iltis_Zugnummernanzeiger_Green.svg", StringComparison.Ordinal);
        }
        return file;
    }

    /// <summary>
    /// Setzt ein Signal auf gruen.
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ToSignalGreen(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return file;
        }
        if (file.Contains("Iltis_Signal_Green.svg", StringComparison.Ordinal)) return file;
        if (file.Contains("Iltis_Signal_Red.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Signal_Red.svg", "Iltis_Signal_Green.svg", StringComparison.Ordinal);
        }
        return file;
    }

    /// <summary>
    /// Setzt ein Signal auf rot.
    /// </summary>
    /// <param name="file">SVG-Dateiname oder Pfad.</param>
    /// <returns>Der angepasste Dateiname.</returns>
    public static string ToSignalRed(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return file;
        }
        if (file.Contains("Iltis_Signal_Red.svg", StringComparison.Ordinal)) return file;
        if (file.Contains("Iltis_Signal_Green.svg", StringComparison.Ordinal))
        {
            return file.Replace("Iltis_Signal_Green.svg", "Iltis_Signal_Red.svg", StringComparison.Ordinal);
        }
        return file;
    }
}
