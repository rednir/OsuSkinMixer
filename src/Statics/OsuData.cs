using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Statics;

public static class OsuData
{
    private const int SWEEP_INTERVAL_MSEC = 1500;

    public static event Action AllSkinsLoaded;

    public static event Action<OsuSkin> SkinAdded;

    public static event Action<OsuSkin> SkinModified;

    public static event Action<OsuSkin> SkinRemoved;

    public static event Action<OsuSkin> SkinInfoRequested;

    public static bool SweepPaused { get; set; } = true;

    public static OsuSkin[] Skins { get => _skins.Keys.OrderBy(s => s.Name).ToArray(); }

    private static Dictionary<OsuSkin, DateTime> _skins;

    static OsuData()
    {
        StartSweepTask();
    }

    public static bool TryLoadSkins()
    {
        SweepPaused = true;
        _skins = new Dictionary<OsuSkin, DateTime>();

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

    public static void AddSkin(OsuSkin skin)
    {
        if (_skins.ContainsKey(skin))
            return;

        _skins.Add(skin, skin.Directory.LastWriteTime);
        Settings.Log($"Added skin to memory: {skin.Name}");
        SkinAdded?.Invoke(skin);
    }

    public static void InvokeSkinModified(OsuSkin skin)
    {
        Settings.Log($"Skin modified: {skin.Name}");
        skin.ClearTextureCache();
        SkinModified?.Invoke(skin);
    }

    public static void RemoveSkin(OsuSkin skin)
    {
        if (!_skins.Remove(skin))
            return;

        Settings.Log($"Removed skin from memory: {skin.Name}");
        SkinRemoved?.Invoke(skin);
    }

    public static void RequestSkinInfo(OsuSkin skin)
    {
        Settings.Log($"Requested skin info: {skin.Name}");
        SkinInfoRequested?.Invoke(skin);
    }

    public static void ToggleSkinHiddenState(OsuSkin skin)
    {
		try
		{
            Directory.CreateDirectory(Settings.HiddenSkinsFolderPath);
			if (skin.Hidden)
			{
				Settings.Log($"Hiding skin: {skin.Name}");
				skin.Directory.MoveTo(Path.Combine(Settings.SkinsFolderPath, skin.Name));
                skin.Hidden = false;
			}
			else
			{
				Settings.Log($"Unhiding skin: {skin.Name}");
				skin.Directory.MoveTo(Path.Combine(Settings.HiddenSkinsFolderPath, skin.Name));
                skin.Hidden = true;
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			OS.Alert("Failed to hide/unhide skin. Please report this issue with logs.", "Error");
		}

        SkinModified?.Invoke(skin);
    }

    private static void LoadSkinsFromDirectory(DirectoryInfo directoryInfo, bool hidden)
    {
        foreach (var dir in directoryInfo.EnumerateDirectories())
        {
            if (!_skins.Any(s => s.Key.Name == directoryInfo.Name) && _skins.TryAdd(new OsuSkin(dir, hidden), dir.LastWriteTime))
                Settings.Log($"Loaded skin into memory: {dir.Name} ({(hidden ? "hidden" : string.Empty)})");
            else
                Settings.Log($"Did not load skin into memory as it already exists: {dir.Name} {(hidden ? "(hidden)" : string.Empty)}");
        }
    }

    private static void StartSweepTask()
    {
        Task.Run(() =>
        {
            Task.Delay(SWEEP_INTERVAL_MSEC).Wait();
            while (true)
            {
                if (!SweepPaused)
                {
                    SweepDirectory(Settings.SkinsFolderPath, false);
                    SweepDirectory(Settings.HiddenSkinsFolderPath, true);
                }

                Task.Delay(SWEEP_INTERVAL_MSEC).Wait();
            }
        });
    }

    private static void SweepDirectory(string directoryPath, bool hidden)
    {
        if (!Directory.Exists(directoryPath) || directoryPath == null)
            return;

        foreach (var pair in _skins)
        {
            if (!Directory.Exists(pair.Key.Directory.FullName))
                RemoveSkin(pair.Key);
        }

        DirectoryInfo skinsFolder = new(directoryPath);
        foreach (var dir in skinsFolder.EnumerateDirectories())
        {
            var pair = _skins.FirstOrDefault(p => p.Key.Directory.Name == dir.Name);

            if (pair.Key == null)
            {
                // Skin was added since the last sweep.
                AddSkin(new OsuSkin(dir, hidden));
                continue;
            }

            if (pair.Value != dir.LastWriteTime)
            {
                // Skin was modified since the last sweep.
                InvokeSkinModified(pair.Key);
                _skins[pair.Key] = dir.LastWriteTime;
            }

            if (pair.Key.Hidden != hidden)
            {
                // Skin changed hidden state since the last sweep.
                InvokeSkinModified(pair.Key);
                pair.Key.Hidden = hidden;
            }
        }
    }
}