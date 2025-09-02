namespace OsuSkinMixer.Statics;

using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Net.Http;
using OsuSkinMixer.Models;
using System.Security.Cryptography;
using System.Text;

public static partial class Settings
{
    public const string VERSION = "v3.0";

    public const string GITHUB_REPO_PATH = "rednir/OsuSkinMixer";

    public static event Action<Exception> ExceptionPushed;

    public static event Action<string> ToastPushed;

    public static string AppdataFolderPath => ProjectSettings.GlobalizePath("user://");

    public static string SettingsFilePath => Path.Combine(AppdataFolderPath, "settings.json");

    public static string LockFilePath => Path.Combine(AppdataFolderPath, "lock");

    public static string EngineOverridesFilePath => (string)ProjectSettings.GetSetting("application/config/project_settings_override");

    public static string SkinsFolderPath => Path.Combine(Content.OsuFolder, "Skins");

    public static string SongsFolderPath => Path.Combine(Content.OsuFolder, "Songs");

    public static string HiddenSkinsFolderPath => Path.Combine(Content.OsuFolder, "HiddenSkins");

    public static string TrashFolderPath => Path.Combine(DeleteOnExitFolderPath, ".osu-skin-mixer-trash");

    public static string TempFolderPath => Path.Combine(Path.GetTempPath(), "osu-skin-mixer");

    public static string AutoUpdateInstallerPath => Path.Combine(TempFolderPath, "osu-skin-mixer-setup.exe");

    public static string DeleteOnExitFolderPath => Path.Combine(TempFolderPath, "delete_on_exit");

    public static string LogFilePath => Path.Combine(TempFolderPath, "very_helpful_logs.txt");

    public static SettingsContent Content { get; set; }

    public static FileStream LockFile { get; set; }

    public static bool IsLoggingToFile => LogFile != null;

    private static StreamWriter LogFile;

    private static readonly HttpClient _httpClient = new();

    public static bool TryCreateLockFile()
    {
        if (!File.Exists(LockFilePath))
            File.Create(LockFilePath).Dispose();

        try
        {
            LockFile = new(LockFilePath, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static void InitialiseSettingsFile()
    {
        Directory.CreateDirectory(TempFolderPath);
        Directory.CreateDirectory(DeleteOnExitFolderPath);

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
            error = "Sorry, osu! skin mixer does not support the lazer client. Please use your osu! stable folder.\n\nIf you do not have osu! stable, you can put your skins in a folder named 'Skins', and set its containing folder as your osu! folder in osu! skin mixer. Make sure each skin is extracted to a folder, and not an .osk file. When you wish to import a skin you've created into osu! lazer, use the 'Export to .osk' button.";
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

        GithubAsset githubAsset = release.Assets.Find(a => a.Name == "osu-skin-mixer-setup.exe");

        if (githubAsset == null)
        {
            PushException(new FileNotFoundException($"Failed to find setup for {release.TagName}"));
            return;
        }

        GD.Print($"Downloading from {githubAsset.BrowserDownloadUrl}");
        await using Stream downloadStream = await _httpClient.GetStreamAsync(githubAsset.BrowserDownloadUrl);

        FileStream fileStream = new(installerPath, FileMode.CreateNew);
        await downloadStream.CopyToAsync(fileStream);
        fileStream.Close();

        string checksum = ComputeSHA256OfFile(installerPath);
        if (checksum != githubAsset?.Digest)
        {
            PushToast("We tried to update osu! skin mixer, but what we downloaded seems corrupted.\n\nYou might have to download the update manually. Sorry!");
            File.Delete(installerPath);
            return;
        }

        Content.UpdatePending = true;
        Save();

        Log($"Finished downloading installer to {installerPath}");
    }

    private static string ComputeSHA256OfFile(string filePath)
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(fileStream);
        StringBuilder sb = new("sha256:");
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }

    public static void StartLoggingToFile()
    {
        if (LogFile != null)
            return;

        LogFile = new StreamWriter(LogFilePath, false);
        LogFile.WriteLine($"****\nNOTE TO USER: File paths in this log file may contain your home folder name.\n****\n\nosu! skin mixer {VERSION} partial logs starting {DateTime.Now}");
    }

    public static void StopLoggingToFile()
    {
        if (LogFile == null)
            return;

        LogFile.WriteLine($"Partial logs ending {DateTime.Now}");
        LogFile.Dispose();
        LogFile = null;

        Tools.ShellOpenFile(TempFolderPath);
    }

    public static void Log(string message)
    {
        string text = $"[{DateTime.Now.ToLongTimeString()}] {message}";

        GD.Print(text);
        LogFile?.WriteLine(text);
    }

    public static void PushException(Exception ex)
    {
        GD.PushError(ex);
        ExceptionPushed?.Invoke(ex);
    }

    public static void PushToast(string message)
    {
        ToastPushed?.Invoke(message);
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

        // Migration from before v2.8.1
        if (lastVersionObject < new Version(2, 8, 1) && Directory.Exists(Path.Combine(AppdataFolderPath, "logs")))
        {
            Directory.Delete(Path.Combine(AppdataFolderPath, "logs"), true);
        }

        // Migration from before v2.8.3
        if (lastVersionObject < new Version(2, 8, 3))
        {
            try
            {
                if (!OsuData.TryLoadSkins())
                    throw new Exception("Failed to load skins for settings data migration.");

                // Estimate the number of skins made by osu! skin mixer.
                Content.SkinsMadeCount = OsuData.Skins.Count(s => s.SkinIni?.TryGetPropertyValue("General", "Author") == OsuSkin.DEFAULT_AUTHOR);
            }
            catch (Exception ex)
            {
                GD.PushError(ex);
            }
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