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

    public const string PresetDay = "Day";
    public const string PresetNight = "Night";
    public const string PresetGaming = "Gaming";
    public const string PresetMovie = "Movie";

    public static readonly string[] BuiltInPresetNames = { PresetDay, PresetNight, PresetGaming, PresetMovie };

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
        s.Presets.TryAdd(PresetDay, new GammaSettings
        {
            Master = 1.0
        });
        s.Presets.TryAdd(PresetNight, new GammaSettings
        {
            Master = 0.85,
            Brightness = -0.05,
            BlueOffset = -0.15
        });
        s.Presets.TryAdd(PresetGaming, new GammaSettings
        {
            Master = 1.15,
            Contrast = 0.10
        });
        s.Presets.TryAdd(PresetMovie, new GammaSettings
        {
            Master = 1.05,
            Brightness = -0.03,
            Contrast = 0.05
        });
    }
}
