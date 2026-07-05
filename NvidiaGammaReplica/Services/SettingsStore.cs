using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NvidiaGammaReplica.Models;

namespace NvidiaGammaReplica.Services;

public sealed class AppSettings
{
    public Dictionary<string, GammaSettings> Monitors { get; set; } = new();
    public Dictionary<string, GammaSettings> Presets { get; set; } = new();
}

public static class SettingsStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NvidiaGammaReplica");

    private static readonly string File = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public const string Preset1 = "Preset 1";
    public const string Preset2 = "Preset 2";
    public const string Preset3 = "Preset 3";
    public const string Preset4 = "Preset 4";

    public static readonly string[] BuiltInPresetNames = { Preset1, Preset2, Preset3, Preset4 };

    public static AppSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(File))
            {
                var json = System.IO.File.ReadAllText(File);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    // Clean up any non-standard preset keys (like legacy "Day", "Night", etc.)
                    var keys = new List<string>(loaded.Presets.Keys);
                    foreach (var key in keys)
                    {
                        if (!BuiltInPresetNames.Contains(key) && !key.StartsWith("Custom "))
                        {
                            loaded.Presets.Remove(key);
                        }
                    }

                    SeedDefaultPresets(loaded);
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Load failed: {ex.Message}");
        }

        var fresh = new AppSettings();
        SeedDefaultPresets(fresh);
        return fresh;
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(settings, JsonOpts);
            System.IO.File.WriteAllText(File, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Save failed: {ex.Message}");
        }
    }

    private static void SeedDefaultPresets(AppSettings s)
    {
        // Preset 1 — neutral / daytime baseline.
        s.Presets.TryAdd(Preset1, new GammaSettings
        {
            Master = 1.0
        });
        // Preset 2 — warm, dimmed night profile (less blue light).
        s.Presets.TryAdd(Preset2, new GammaSettings
        {
            Master = 0.85,
            Brightness = -0.05,
            BlueOffset = -0.15
        });
        // Preset 3 — punchy, high-contrast gaming profile.
        s.Presets.TryAdd(Preset3, new GammaSettings
        {
            Master = 1.15,
            Contrast = 0.10
        });
        // Preset 4 — cinematic, slightly darker with mild contrast.
        s.Presets.TryAdd(Preset4, new GammaSettings
        {
            Master = 1.05,
            Brightness = -0.03,
            Contrast = 0.05
        });
    }
}
