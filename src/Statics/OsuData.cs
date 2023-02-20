using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.Statics;

public static class OsuData
{
    public static Skin[] Skins { get => _skins.OrderBy(s => s.Name).ToArray(); }

    private static List<Skin> _skins;

    public static bool TryLoadSkins()
    {
        _skins = new List<Skin>();

        if (Settings.Content.SkinsFolder == null || !Directory.Exists(Settings.Content.SkinsFolder))
            return false;

        var skinsFolder = new DirectoryInfo(Settings.Content.SkinsFolder);

        foreach (var dir in skinsFolder.EnumerateDirectories())
        {
            if (dir.Name != SkinCreator.WORKING_DIR_NAME)
                _skins.Add(new Skin(dir));
        }

        GD.Print($"Loaded {_skins.Count} skins into memory.");
        return true;
    }

    public static void AddSkin(Skin skin)
    {
        _skins.Add(skin);
        GD.Print($"Added skin '{skin.Name}' to memory.");
    }
}