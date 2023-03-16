using Godot;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using File = System.IO.File;
using Directory = System.IO.Directory;
using HttpClient = System.Net.Http.HttpClient;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Statics;

public static partial class Settings
{
    public const string VERSION = "v2.4.1";

    public const string GITHUB_REPO_PATH = "rednir/OsuSkinMixer";

    public static event Action<Exception> ExceptionPushed;

    public static string SettingsFilePath => ProjectSettings.GlobalizePath("user://settings.json");

    public static string SkinsFolderPath => $"{Content.OsuFolder}/Skins/";

    public static string HiddenSkinsFolderPath => $"{Content.OsuFolder}/HiddenSkins/";

    public static string TrashFolderPath => Path.Combine(Content.OsuFolder, ".osu-skin-mixer-trash");

    public static string TempFolderPath => Path.Combine(Path.GetTempPath(), "osu-skin-mixer");

    public static string AutoUpdateInstallerPath => Path.Combine(TempFolderPath, "osu-skin-mixer-setup.exe");

    public static SettingsContent Content { get; set; }

    private static readonly HttpClient _httpClient = new();

    public static void InitialiseSettingsFile()
    {
        Directory.CreateDirectory(TempFolderPath);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "osu! skin mixer");
        _httpClient.Timeout = TimeSpan.FromSeconds(300);

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
                GD.PushError($"Failed to deserialize {SettingsFilePath} due to exception {ex}");
                Log($"SETTINGS:\n{File.ReadAllText(SettingsFilePath)}");
                File.Delete(SettingsFilePath + ".bak");
                File.Move(SettingsFilePath, SettingsFilePath + ".bak");
            }
        }

        Content = new SettingsContent();
        CheckForAutoUpdateFlag();
        Save();
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(
                SettingsFilePath, JsonSerializer.Serialize(Content, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            PushException(new IOException("Failed to save settings file.", ex));
        }
    }

    public static bool TrySetOsuFolder(string path, out string error)
    {
        if (!Directory.Exists(path))
        {
            error = "The specified folder doesn't exist.";
            return false;
        }

        if (path.EndsWith("Skins") || path.EndsWith("Skins/") || path.EndsWith("Skins\\"))
        {
            error = "Make sure you're pointing to the osu! folder, not your skins folder.";
            return false;
        }

        if (File.Exists($"{path}/client.realm"))
        {
            error = "osu! skin mixer does not support the lazer client yet, please use your osu! stable folder.";
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

    public static async Task<GithubRelease> GetLatestReleaseOrNullAsync()
    {
        await using Stream stream = await _httpClient.GetStreamAsync($"https://api.github.com/repos/{GITHUB_REPO_PATH}/releases/latest");
        GithubRelease release = await JsonSerializer.DeserializeAsync<GithubRelease>(stream);
        Log($"Got latest release: {release.TagName}");

        return release.TagName != VERSION ? release : null;
    }

    public static async Task DownloadInstallerAsync(GithubRelease release)
    {
        string installerPath = Path.Combine(TempFolderPath, "osu-skin-mixer-setup.exe");

        if (File.Exists(installerPath))
        {
            // The last update attempt was probably canceled or failed.
            File.Delete(installerPath);
            return;
        }

        Log($"Downloading installer for {release.TagName}");

        await using Stream downloadStream = await _httpClient.GetStreamAsync($"https://github.com/{GITHUB_REPO_PATH}/releases/latest/download/osu-skin-mixer-setup.exe");

        using FileStream fileStream = new(installerPath, FileMode.CreateNew);
        await downloadStream.CopyToAsync(fileStream);

        Log($"Finished downloading installer to {installerPath}");
    }

    public static void Log(string message)
    {
        GD.Print($"[{DateTime.Now.ToLongTimeString()}] {message}");
    }

    public static void PushException(Exception ex)
    {
        GD.PushError(ex);
        ExceptionPushed?.Invoke(ex);
    }

    private static void MigrateSettings()
    {
        // Migration from before v2.1.0
        if (Content.SkinsFolder != null)
        {
            string osuFolder = Content.SkinsFolder.TrimEnd('/').TrimEnd('\\').TrimSuffix("Skins");
            Content.OsuFolder = osuFolder;
            Content.SkinsFolder = null;

            // Avoid prompting the user to confirm their osu! folder.
            if (TrySetOsuFolder(osuFolder, out _))
                Save();
        }

        // Migration from before v2.3.0
        if (Content.OsuFolder != null && Content.LastVersion == null)
        {
            Content.LastVersion = "v1.0.0";
            Save();
            return;
        }

        // Migration from before v2.4.0
        if (Version.TryParse(Content.LastVersion.TrimStart('v'), out Version lastVersionObject)
            && lastVersionObject < new Version(2, 4, 0))
        {
            CheckForAutoUpdateFlag();
            Save();
        }
    }

    private static void CheckForAutoUpdateFlag()
    {
        // Find out if we are running from an executable created by the installer, if so enable auto update by default.
        string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (executableDirectory == null)
            return;

        Content.AutoUpdate = File.Exists(Path.Combine(executableDirectory, "..", "auto-update"));
    }
}