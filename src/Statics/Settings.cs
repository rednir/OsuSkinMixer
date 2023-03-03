using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using File = System.IO.File;
using Directory = System.IO.Directory;

namespace OsuSkinMixer.Statics;

public static class Settings
{
    public const string VERSION = "v2.1.0";

    public const string GITHUB_REPO_PATH = "rednir/OsuSkinMixer";

    public static string SettingsFilePath => ProjectSettings.GlobalizePath("user://settings.json");

    public static string SkinsFolderPath => $"{Content.OsuFolder}/Skins/";

    static Settings()
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                Content = JsonSerializer.Deserialize<SettingsContent>(File.ReadAllText(SettingsFilePath));
                MigrateSettings();
                return;
            }
            catch (Exception ex)
            {
                GD.PushError($"Failed to deserialize {SettingsFilePath} due to exception {ex.Message}");
                OS.Alert("Your settings have beeen corrupted, please report this issue and attach logs. All settings have been reset.", "Error");
                GD.Print($"SETTINGS:\n{File.ReadAllText(SettingsFilePath)}");
                File.Delete(SettingsFilePath + ".bak");
                File.Move(SettingsFilePath, SettingsFilePath + ".bak");
            }
        }

        Content = new SettingsContent();
        Save();
    }

    public static SettingsContent Content { get; set; }

    public static void Save()
    {
        try
        {
            File.WriteAllText(
                SettingsFilePath, JsonSerializer.Serialize(Content, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            OS.Alert($"Couldn't save settings file due to the following error:\n\n'{ex.Message}'\n\nPlease make sure the program has sufficient privileges. Your settings will not be saved this session.");
        }
    }

    public static bool TrySetOsuFolder(string path, out string error)
    {
        if (!Directory.Exists(path))
        {
            error = "The specified folder doesn't exist.";
            return false;
        }

        if (path.EndsWith("Skins") || path.EndsWith("Skins/"))
        {
            error = "Make sure you're pointing to the osu! folder, not your skins folder.";
            return false;
        }

        if (!Directory.Exists($"{path}/Skins"))
        {
            error = "We couldn't find a 'Skins' folder in the specified folder, please make sure you're pointing to a valid osu! folder.\n\nIf you are struggling to find your osu! folder, open osu! and search for 'Open osu! folder' in the options menu.";
            return false;
        }

        Content.OsuFolder = path;
        error = null;
        return OsuData.TryLoadSkins();
    }

    private static void MigrateSettings()
    {
        // Migration from v2.0.0 or earlier.
        if (Content.SkinsFolder != null)
        {
            string osuFolder = Content.SkinsFolder.TrimEnd('/').TrimSuffix("Skins");
            Content.OsuFolder = osuFolder;
            Content.SkinsFolder = null;

            // Avoid prompting the user to confirm their osu! folder.
            if (TrySetOsuFolder(osuFolder, out _))
                Save();
        }
    }

    public class SettingsContent
    {
        [JsonPropertyName("skins_folder")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SkinsFolder { get; set; }

        [JsonPropertyName("osu_folder")]
        public string OsuFolder { get; set; }

        [JsonPropertyName("import_to_game_if_open")]
        public bool ImportToGameIfOpen { get; set; } = OS.GetName() == "Windows";

        [JsonPropertyName("arrow_button_pressed")]
        public bool ArrowButtonPressed { get; set; }
    }
}