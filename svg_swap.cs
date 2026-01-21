using System;
using System.IO;
using System.Security.Cryptography;

namespace OpenTrainDrive.Tools;

/// <summary>
/// Kommandozeilenwerkzeug zum Tauschen oder Umschalten von SVG-Dateien.
/// </summary>
static class SvgSwap
{
    /// <summary>
    /// Einstiegspunkt der Anwendung.
    /// </summary>
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return 0;
        }

        if (args.Length == 1)
        {
            try
            {
                return RunAuto(args[0]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fehler: {ex.Message}");
                return 2;
            }
        }

        var mode = args[0].ToLowerInvariant();
        try
        {
            return mode switch
            {
                "auto" => RunAuto(args[1]),
                "swap" => RunSwap(args),
                "toggle" => RunToggle(args),
                _ => Fail("Unbekannter Modus. Verwende 'swap' oder 'toggle'.")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler: {ex.Message}");
            return 2;
        }
    }

    static int RunSwap(string[] args)
    {
        if (args.Length < 3)
        {
            return Fail("swap erwartet: swap <fileA.svg> <fileB.svg>");
        }

        var fileA = args[1];
        var fileB = args[2];
        EnsureExists(fileA);
        EnsureExists(fileB);

        var temp = Path.GetTempFileName();
        File.Copy(fileA, temp, true);
        File.Copy(fileB, fileA, true);
        File.Copy(temp, fileB, true);
        File.Delete(temp);

        Console.WriteLine("SVGs getauscht.");
        return 0;
    }

    static int RunToggle(string[] args)
    {
        if (args.Length < 4)
        {
            return Fail("toggle erwartet: toggle <target.svg> <variantA.svg> <variantB.svg>");
        }

        var target = args[1];
        var variantA = args[2];
        var variantB = args[3];
        EnsureExists(variantA);
        EnsureExists(variantB);

        var targetBytes = File.Exists(target) ? File.ReadAllBytes(target) : Array.Empty<byte>();
        var hashTarget = Hash(targetBytes);
        var hashA = Hash(File.ReadAllBytes(variantA));
        var hashB = Hash(File.ReadAllBytes(variantB));

        if (HashesEqual(hashTarget, hashA))
        {
            File.Copy(variantB, target, true);
            Console.WriteLine("Target auf Variante B gesetzt.");
            return 0;
        }
        if (HashesEqual(hashTarget, hashB))
        {
            File.Copy(variantA, target, true);
            Console.WriteLine("Target auf Variante A gesetzt.");
            return 0;
        }

        File.Copy(variantA, target, true);
        Console.WriteLine("Target war unbekannt, Variante A gesetzt.");
        return 0;
    }

    static void PrintHelp()
    {
        Console.WriteLine("SVG Swap Utility");
        Console.WriteLine("  <target.svg>                      Automatisch zu vordefiniertem Gegenstueck wechseln");
        Console.WriteLine("  auto <target.svg>                 Wie oben");
        Console.WriteLine("  swap <fileA.svg> <fileB.svg>        Tauscht die Inhalte der beiden Dateien");
        Console.WriteLine("  toggle <target.svg> <a.svg> <b.svg> Wechselt target zwischen a und b");
    }

    static void EnsureExists(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Datei nicht gefunden", path);
        }
    }

    static byte[] Hash(byte[] data)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(data);
    }

    static bool HashesEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        PrintHelp();
        return 1;
    }

    static int RunAuto(string target)
    {
        EnsureExists(target);
        var dir = Path.GetDirectoryName(target) ?? ".";
        var name = Path.GetFileName(target);

        string? candidate = null;

        if (name.Contains("_Red.svg", StringComparison.Ordinal))
        {
            candidate = name.Replace("_Red.svg", "_Green.svg", StringComparison.Ordinal);
        }
        else if (name.Contains("_Green.svg", StringComparison.Ordinal))
        {
            var red = name.Replace("_Green.svg", "_Red.svg", StringComparison.Ordinal);
            var white = name.Replace("_Green.svg", "_White.svg", StringComparison.Ordinal);
            if (File.Exists(Path.Combine(dir, red)))
            {
                candidate = red;
            }
            else if (File.Exists(Path.Combine(dir, white)))
            {
                candidate = white;
            }
        }
        else if (name.Contains("_White.svg", StringComparison.Ordinal))
        {
            var green = name.Replace("_White.svg", "_Green.svg", StringComparison.Ordinal);
            if (File.Exists(Path.Combine(dir, green)))
            {
                candidate = green;
            }
        }
        else if (name.Contains("_Off.svg", StringComparison.Ordinal))
        {
            candidate = name.Replace("_Off.svg", "_On.svg", StringComparison.Ordinal);
        }
        else if (name.Contains("_On.svg", StringComparison.Ordinal))
        {
            candidate = name.Replace("_On.svg", "_Off.svg", StringComparison.Ordinal);
        }
        else if (name.Contains("_off.svg", StringComparison.Ordinal))
        {
            candidate = name.Replace("_off.svg", "_on.svg", StringComparison.Ordinal);
        }
        else if (name.Contains("_on.svg", StringComparison.Ordinal))
        {
            candidate = name.Replace("_on.svg", "_off.svg", StringComparison.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Fail("Kein vordefiniertes Gegenstueck gefunden.");
        }

        var otherPath = Path.Combine(dir, candidate);
        EnsureExists(otherPath);
        File.Copy(otherPath, target, true);
        Console.WriteLine($"Target gewechselt: {name} -> {candidate}");
        return 0;
    }
}
