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
    public static Skin[] Skins { get; private set; }

    public static bool TryLoadSkins()
    {
        if (Settings.Content.SkinsFolder == null || !Directory.Exists(Settings.Content.SkinsFolder))
            return false;

        var result = new List<Skin>();
        var skinsFolder = new DirectoryInfo(Settings.Content.SkinsFolder);

        foreach (var dir in skinsFolder.EnumerateDirectories())
        {
            if (dir.Name != SkinCreator.WORKING_DIR_NAME)
                result.Add(new Skin(dir));
        }

        GD.Print($"Loaded {result.Count} skins");

        Skins = result.OrderBy(s => s.Name).ToArray();
        return true;
    }
}