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

        Settings.Log($"About to load all skins into memory from {Settings.SkinsFolderPath}");

        var skinsFolder = new DirectoryInfo(Settings.SkinsFolderPath);

        foreach (var dir in skinsFolder.EnumerateDirectories())
        {
            if (_skins.TryAdd(new OsuSkin(dir), dir.LastWriteTime))
                Settings.Log($"Loaded skin into memory: {dir.Name}");
            else
                Settings.Log($"Did not load skin into memory as it already exists: {dir.Name}");
        }

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

    private static void StartSweepTask()
    {
        Task.Run(() =>
        {
            Task.Delay(SWEEP_INTERVAL_MSEC).Wait();
            while (true)
            {
                if (!SweepPaused)
                    SweepSkinsFolder();

                Task.Delay(SWEEP_INTERVAL_MSEC).Wait();
            }
        });
    }

    private static void SweepSkinsFolder()
    {
        if (!Directory.Exists(Settings.SkinsFolderPath))
            return;

        foreach (var pair in _skins)
        {
            if (!Directory.Exists(pair.Key.Directory.FullName))
                RemoveSkin(pair.Key);
        }

        DirectoryInfo skinsFolder = new(Settings.SkinsFolderPath);
        foreach (var dir in skinsFolder.EnumerateDirectories())
        {
            var pair = _skins.FirstOrDefault(p => p.Key.Directory.Name == dir.Name);

            if (pair.Key == null)
            {
                // Skin was added since the last sweep.
                AddSkin(new OsuSkin(dir));
                continue;
            }

            if (pair.Value != dir.LastWriteTime)
            {
                // Skin was modified since the last sweep.
                InvokeSkinModified(pair.Key);
                _skins[pair.Key] = dir.LastWriteTime;
            }
        }
    }
}