namespace OsuSkinMixer.Statics;

using System.IO;
using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Models.Realm;
using Realms;

/// <summary>
/// A static class that provides other objects with the user's osu! data, such as their list of skins.
/// However, this class will never peform write operations in the user's osu! folder.
/// </summary>
public static class OsuData
{
    private const int SWEEP_INTERVAL_MSEC = 1500;

    public static event Action AllSkinsLoaded;

    public static event Action<OsuSkinBase> SkinAdded;

    public static event Action<OsuSkinBase> SkinModified;

    public static event Action<OsuSkinBase> SkinRemoved;

    public static event Action<IEnumerable<OsuSkinBase>> SkinInfoRequested;

    public static event Action<IEnumerable<OsuSkinBase>> SkinModifyRequested;

    public static event Action<OsuSkinBase, OsuSkinBase> SkinConflictDetected;

    public static bool SweepPaused { get; set; } = true;

    public static bool IsLazer => Settings.Content.IsLazer;

    public static RealmConfiguration RealmConfigReadOnly
        => new(Path.Combine(Settings.Content.OsuFolder, "client.realm"))
        {
            IsReadOnly = true,
            SchemaVersion = 51,
        };

    public static RealmConfiguration RealmConfigWriteable
    {
        get
        {
            RealmConfiguration result = RealmConfigReadOnly;
            result.IsReadOnly = false;
            return result;
        }
    }

    public static OsuSkinBase[] Skins { get => _skins.Keys.OrderBy(s => s.Name).ToArray(); }

    private static Dictionary<OsuSkinBase, DateTime> _skins;

    static OsuData()
    {
        StartSweepTask();
    }

    public static bool TryLoadSkins()
    {
        SweepPaused = true;
        _skins = new Dictionary<OsuSkinBase, DateTime>();

        if (Settings.Content.OsuFolder == null || !Directory.Exists(Settings.SkinsFolderPath))
            return false;

        Settings.Log($"About to load all skins into memory from {Settings.Content.OsuFolder}");

        LoadSkinsFromDirectory(new DirectoryInfo(Settings.SkinsFolderPath), false);

        if (Directory.Exists(Settings.HiddenSkinsFolderPath))
            LoadSkinsFromDirectory(new DirectoryInfo(Settings.HiddenSkinsFolderPath), true);

        AllSkinsLoaded?.Invoke();
        SweepPaused = false;
        return true;
    }

    public static bool TryLoadSkinsLazer()
    {
        Settings.Log($"About to load skins into memory from lazer directory {Settings.Content.OsuFolder}");

        SweepPaused = true;
        _skins = new Dictionary<OsuSkinBase, DateTime>();

        try
        {        
            using Realm realm = Realm.GetInstance(RealmConfigReadOnly);

            foreach (var realmSkin in realm.All<RealmOsuSkin>())
                _skins.TryAdd(new OsuSkinLazer(realmSkin), default);
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex);
            return false;
        }

        AllSkinsLoaded?.Invoke();
        return true;
    }
    
    public static OsuSkinStable CreateStableSkinFromLazer(OsuSkinLazer lazerSkin)
    {
        DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Settings.LazerConvertsFolderPath, lazerSkin.Name));
        OsuSkinStable stableSkin = new(lazerSkin.Name, directory, false, lazerSkin.ID);

        // Ensure the skin directory is empty first.
        foreach (var file in directory.EnumerateFiles())
            file.Delete();
        foreach (var dir in directory.EnumerateDirectories())
            dir.Delete(true);

        foreach (OsuSkinFile file in lazerSkin.Files)
        {
            string fileDestinationPath = Path.Combine(directory.FullName, file.VirtualPath);

            Settings.Log($"Copying '{file.PhysicalPath}' -> '{fileDestinationPath}'");

            // Ensure the containing directory exists before copying, skins often have subfolders for extras etc.
            Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath));
            File.Copy(file.PhysicalPath, fileDestinationPath, true);
        }

        // TODO: move this log to skin machine logs.
        Settings.Log($"Converted lazer skin '{lazerSkin.Name}' to stable skin at '{directory.FullName}'");
        Tools.ShellOpenFile(directory.FullName);

        return stableSkin;
    }

    public static void AddSkin(OsuSkinBase skin)
    {
        lock (_skins)
        {
            if (skin is OsuSkinStable stableSkin)
            {
                if (_skins.ContainsKey(stableSkin))
                    return;

                _skins.Add(stableSkin, stableSkin.LastWriteTime);
                Settings.Log($"Added skin to memory: {stableSkin.Name}");
                SkinAdded?.Invoke(stableSkin);
            }
        }
    }

    public static void InvokeSkinModified(OsuSkinBase skin)
    {
        lock (_skins)
        {
            if (skin is OsuSkinStable stableSkin)
            {
                Settings.Log($"Skin modified: {stableSkin.Name}");
                //stableSkin.ClearCache();
                _skins[stableSkin] = stableSkin.LastWriteTime;
                SkinModified?.Invoke(stableSkin);
            }
        }
    }

    public static void RemoveSkin(OsuSkinBase skin)
    {
        lock (_skins)
        {
            if (!_skins.Remove(skin))
                return;

            Settings.Log($"Removed skin from memory: {skin.Name}");
            SkinRemoved?.Invoke(skin);
        }
    }

    public static void RequestSkinInfo(IEnumerable<OsuSkinBase> skins)
    {
        lock (_skins)
        {
            Settings.Log($"Requested skin info for {skins.Count()} skins.");
            SkinInfoRequested?.Invoke(skins);
        }
    }

    public static void RequestSkinModify(IEnumerable<OsuSkinBase> skins)
    {
        lock (_skins)
        {
            Settings.Log($"Requested skin modify for {skins.Count()} skins.");
            SkinModifyRequested?.Invoke(skins);
        }
    }

    private static void LoadSkinsFromDirectory(DirectoryInfo directoryInfo, bool hidden)
    {
        lock (_skins)
        {
            foreach (var dir in directoryInfo.EnumerateDirectories())
            {
                if (!_skins.Any(s => s.Key.Name == directoryInfo.Name) && _skins.TryAdd(new OsuSkinStable(dir, hidden), dir.LastWriteTime))
                    Settings.Log($"Loaded skin into memory: {dir.Name} {(hidden ? "(hidden)" : string.Empty)}");
                else
                    Settings.Log($"Did not load skin into memory as it already exists: {dir.Name} {(hidden ? "(hidden)" : string.Empty)}");
            }
        }
    }

    private static void StartSweepTask()
    {
        Task.Run(() =>
        {
            GodotThread.SetThreadSafetyChecksEnabled(false);
            Task.Delay(SWEEP_INTERVAL_MSEC).Wait();
            while (true)
            {
                if (!SweepPaused)
                {
                    lock (_skins)
                    {
                        SweepSkins(false);
                        SweepSkins(true);
                    }
                }

                Task.Delay(SWEEP_INTERVAL_MSEC).Wait();
            }
        });
    }

    private static void SweepSkins(bool hidden)
    {
        // string path = hidden ? Settings.HiddenSkinsFolderPath : Settings.SkinsFolderPath;

        // if (!Directory.Exists(path) || path == null)
        //     return;

        // foreach (var pair in _skins)
        // {
        //     if (!Directory.Exists(pair.Key.Directory.FullName))
        //         RemoveSkin(pair.Key);
        // }

        // DirectoryInfo skinsFolder = new(path);
        // foreach (var dir in skinsFolder.EnumerateDirectories())
        // {
        //     var pair = _skins.FirstOrDefault(p => p.Key.Directory.Name == dir.Name);

        //     if (pair.Key == null)
        //     {
        //         // Skin was added since the last sweep.
        //         AddSkin(new OsuSkin(dir, hidden));
        //         continue;
        //     }

        //     if (pair.Key.Hidden != hidden)
        //     {
        //         string visibleSkinPath = Path.Combine(Settings.SkinsFolderPath, pair.Key.Name);
        //         string hiddenSkinPath = Path.Combine(Settings.HiddenSkinsFolderPath, pair.Key.Name);
        //         if (Directory.Exists(visibleSkinPath) && Directory.Exists(hiddenSkinPath))
        //         {
        //             // There is a skin with the same name as the hidden skin in the visible skins folder.
        //             Settings.Log($"Skin conflict detected for skin: {pair.Key.Name}");
        //             SkinConflictDetected?.Invoke(
        //                 new OsuSkin(new DirectoryInfo(visibleSkinPath), false),
        //                 new OsuSkin(new DirectoryInfo(hiddenSkinPath), true));

        //             SweepPaused = true;
        //             return;
        //         }

        //         // Skin changed hidden state since the last sweep.
        //         InvokeSkinModified(pair.Key);
        //         pair.Key.Hidden = hidden;
        //     }

        //     if (pair.Value != dir.LastWriteTime)
        //     {
        //         // Skin was modified since the last sweep.
        //         InvokeSkinModified(pair.Key);
        //         _skins[pair.Key] = dir.LastWriteTime;
        //     }
        // }
    }
}