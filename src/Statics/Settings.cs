using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Environment = System.Environment;
using File = System.IO.File;
using Directory = System.IO.Directory;

namespace OsuSkinMixer
{
    public static class Settings
    {
        public const string VERSION = "v1.7";

        public static readonly string AppdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu-skin-mixer";

        public static string SettingsFilePath => AppdataPath + "/settings.json";

        public static string LogFilePath => AppdataPath + "/log.txt";

        static Settings()
        {
            Directory.CreateDirectory(AppdataPath);

            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    Content = JsonSerializer.Deserialize<SettingsContent>(File.ReadAllText(SettingsFilePath));
                    return;
                }
                catch (Exception ex)
                {
                    OS.Alert($"Couldn't load your settings file due to the following error:\n\n'{ex.Message}'\n\nYour broken settings file will be renamed and a new one will replace it.");
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

        public class SettingsContent
        {
            [JsonPropertyName("skins_folder")]
            public string SkinsFolder { get; set; }

            [JsonPropertyName("log_to_file")]
            public bool LogToFile { get; set; }

            [JsonPropertyName("import_to_game_if_open")]
            public bool ImportToGameIfOpen { get; set; } = true;
        }
    }
}