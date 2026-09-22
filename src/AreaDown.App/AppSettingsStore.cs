using System.IO;
using System.Text.Json;
using AreaDown.Core;

namespace AreaDown.App;

/// <summary>
/// Persisted application settings stored in %APPDATA%/AreaDown/settings.json.
/// </summary>
public sealed record AppSettings(
    string SaveFolder,
    string LastQuality,
    string? LastUrl)
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AreaDown", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                    ?? Default();
            }
        }
        catch
        {
            // Corrupted settings — start fresh
        }
        return Default();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Best effort — settings are non-critical
        }
    }

    public static AppSettings Default() => new(
        SaveFolder: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
        LastQuality: "best",
        LastUrl: null
    );
}