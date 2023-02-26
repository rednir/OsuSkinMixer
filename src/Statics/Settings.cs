using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using File = System.IO.File;
using Directory = System.IO.Directory;

namespace OsuSkinMixer.Statics;

public static class Settings
{
    public const string VERSION = "v2.0.0";

    public const string GITHUB_REPO_PATH = "rednir/OsuSkinMixer";

    public static string SettingsFilePath => ProjectSettings.GlobalizePath("user://settings.json");

    static Settings()
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                Content = JsonSerializer.Deserialize<SettingsContent>(File.ReadAllText(SettingsFilePath));
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

    public static bool TrySetSkinsFolder(string path)
    {
        if (Directory.Exists(path))
        {
            Content.SkinsFolder = path;
            return true;
        }

        return false;
    }

    public class SettingsContent
    {
        [JsonPropertyName("skins_folder")]
        public string SkinsFolder { get; set; }

        [JsonPropertyName("import_to_game_if_open")]
        public bool ImportToGameIfOpen { get; set; } = OS.GetName() == "Windows";

        [JsonPropertyName("arrow_button_pressed")]
        public bool ArrowButtonPressed { get; set; }
    }
}