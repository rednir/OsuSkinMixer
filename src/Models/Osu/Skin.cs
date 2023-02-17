using System;
using System.IO;
using Godot;
using File = System.IO.File;

namespace OsuSkinMixer;

public class Skin
{
    public Skin()
    {
    }

    public Skin(DirectoryInfo dir)
    {
        Name = dir.Name;
        Directory = dir;
        if (File.Exists($"{dir.FullName}/skin.ini"))
        {
            try
            {
                SkinIni = new SkinIni(File.ReadAllText($"{dir.FullName}/skin.ini"));
            }
            catch (Exception ex)
            {
                GD.PushError($"Failed to parse skin.ini for skin '{Name}' due to exception {ex.Message}");
                OS.Alert($"Skin.ini parse error for skin '{Name}', please report this error!\n\n{ex.Message}");
            }
        }
    }

    public string Name { get; set; }

    public DirectoryInfo Directory { get; set; }

    public SkinIni SkinIni { get; set; }
}