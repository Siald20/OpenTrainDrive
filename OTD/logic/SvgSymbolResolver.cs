using System;
using System.Collections.Generic;
using System.IO;

namespace OTD.logic.svgAPI;

/// <summary>
/// Hilfsfunktionen zum Auflosen von SVG-Dateien anhand von Elementname und Farbe.
/// </summary>
public static class SvgSymbolResolver
{
    private static readonly Dictionary<string, string> ColorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "red", "Red" },
        { "rot", "Red" },
        { "green", "Green" },
        { "gruen", "Green" },
        { "grün", "Green" },
        { "white", "White" },
        { "weiss", "White" },
        { "weiß", "White" },
        { "magenta", "Magenta" },
        { "yellow", "Yellow" },
        { "gelb", "Yellow" },
        { "on", "On" },
        { "off", "Off" }
    };

    private static readonly string[] KnownSuffixes =
    {
        "_Red", "_Green", "_White", "_Magenta", "_Yellow",
        "_On", "_Off", "_on", "_off"
    };

    /// <summary>
    /// Sucht eine passende SVG-Datei im Verzeichnis und gibt den Dateinamen zuruck.
    /// </summary>
    /// <param name="elementName">Symbolname ohne feste Farbsuffixe.</param>
    /// <param name="color">Farbe oder Farbcode.</param>
    /// <param name="svgDirectory">Basisverzeichnis der SVG-Dateien.</param>
    public static string? ResolveSvgFile(string elementName, string color, string svgDirectory)
    {
        if (string.IsNullOrWhiteSpace(elementName) || string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var baseName = NormalizeElementName(elementName);
        var colorToken = NormalizeColor(color);
        if (string.IsNullOrWhiteSpace(baseName) || string.IsNullOrWhiteSpace(colorToken))
        {
            return null;
        }

        var candidates = new List<string>
        {
            $"{baseName}_{colorToken}.svg"
        };

        if (!baseName.StartsWith("Iltis_", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add($"Iltis_{baseName}_{colorToken}.svg");
        }

        foreach (var file in candidates)
        {
            var path = Path.Combine(svgDirectory, file);
            if (File.Exists(path))
            {
                return file;
            }
        }

        if (!Directory.Exists(svgDirectory))
        {
            return null;
        }

        var files = Directory.GetFiles(svgDirectory, "*.svg");
        foreach (var filePath in files)
        {
            var file = Path.GetFileName(filePath);
            if (file.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0 &&
                file.IndexOf(colorToken, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return file;
            }
        }

        return null;
    }

    /// <summary>
    /// Wie ResolveSvgFile, aber mit SVG-Pfad-Prefix.
    /// </summary>
    /// <param name="elementName">Symbolname ohne feste Farbsuffixe.</param>
    /// <param name="color">Farbe oder Farbcode.</param>
    /// <param name="svgDirectory">Basisverzeichnis der SVG-Dateien.</param>
    public static string? ResolveSvgPath(string elementName, string color, string svgDirectory)
    {
        var file = ResolveSvgFile(elementName, color, svgDirectory);
        return file == null ? null : $"/svg/{file}";
    }

    private static string NormalizeElementName(string elementName)
    {
        var name = Path.GetFileNameWithoutExtension(elementName).Trim();
        if (name.Length == 0)
        {
            return name;
        }

        foreach (var suffix in KnownSuffixes)
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - suffix.Length);
                break;
            }
        }

        name = name.TrimEnd('_', '-', ' ');
        return name.Replace(' ', '_');
    }

    private static string NormalizeColor(string color)
    {
        var cleaned = color.Trim().Replace(' ', '_');
        if (cleaned.Length == 0)
        {
            return cleaned;
        }

        if (ColorMap.TryGetValue(cleaned, out var mapped))
        {
            return mapped;
        }

        return char.ToUpperInvariant(cleaned[0]) + cleaned.Substring(1);
    }
}
