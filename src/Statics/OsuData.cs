using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using OsuSkinMixer.Models.Osu;

namespace OsuSkinMixer.Statics;

public static class OsuData
{
    public static OsuSkin[] Skins { get => _skins.OrderBy(s => s.Name).ToArray(); }

    private static FileSystemWatcher FileSystemWatcher { get; set; }

    private static List<OsuSkin> _skins;

    public static bool TryLoadSkins()
    {
        _skins = new List<OsuSkin>();

        if (Settings.Content.SkinsFolder == null || !Directory.Exists(Settings.Content.SkinsFolder))
            return false;

        var skinsFolder = new DirectoryInfo(Settings.Content.SkinsFolder);

        foreach (var dir in skinsFolder.EnumerateDirectories())
        {
            if (dir.Name != SkinCreator.WORKING_DIR_NAME)
                _skins.Add(new OsuSkin(dir));
        }

        GD.Print($"Loaded {_skins.Count} skins into memory.");
        SetupWatcher();

        return true;
    }

    public static void AddSkin(OsuSkin skin)
    {
        _skins.Add(skin);
        GD.Print($"Added skin '{skin.Name}' to memory.");
    }

    private static void SetupWatcher()
    {
        FileSystemWatcher = new FileSystemWatcher(Settings.Content.SkinsFolder)
        {
            NotifyFilter = NotifyFilters.CreationTime
                            | NotifyFilters.DirectoryName
                            | NotifyFilters.FileName
                            | NotifyFilters.LastWrite
                            | NotifyFilters.Attributes
                            | NotifyFilters.LastAccess
                            | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        FileSystemWatcher.Changed += (s, e) =>
        {
            if (e.Name == SkinCreator.WORKING_DIR_NAME
                || e.ChangeType != WatcherChangeTypes.Changed
                || e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }

            _skins.RemoveAll(s => s.Directory.Name == e.Name);

            var skin = new OsuSkin(new DirectoryInfo(e.FullPath));
            _skins.Add(skin);
            GD.Print($"Added skin '{skin.Name}' to memory.");
        };

        FileSystemWatcher.Deleted += (s, e) =>
        {
            if (e.Name == SkinCreator.WORKING_DIR_NAME)
                return;

            var skin = _skins.Find(s => s.Directory.Name == e.Name);
            if (skin != null)
            {
                _skins.Remove(skin);
                GD.Print($"Removed skin '{skin.Name}' from memory.");
            }
        };

        FileSystemWatcher.Renamed += (s, e) =>
        {
            var skin = _skins.Find(s => s.Directory.Name == e.OldName);
            if (skin != null)
            {
                skin.Name = e.Name;
                skin.Directory = new DirectoryInfo(e.FullPath);
                GD.Print($"Renamed skin '{e.OldName}' to '{e.Name}' in memory.");
            }
        };

        FileSystemWatcher.Error += (s, e) => GD.PrintErr($"FileSystemWatcher error: {e.GetException()}");
    }
}