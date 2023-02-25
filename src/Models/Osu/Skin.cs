using System;
using System.IO;
using Godot;
using File = System.IO.File;

namespace OsuSkinMixer.Models.Osu;

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

    public Texture2D HitcircleTexture => GetTexture("hitcircle.png");

    public Texture2D HitcircleoverlayTexture => GetTexture("hitcircleoverlay.png");

    public Texture2D Default1Texture => GetTexture("default-1.png");

    private Texture2D GetTexture(string filename)
    {
        string path = $"{Directory.FullName}/{filename}";
		Image image = new();

        if (!File.Exists(path))
            return GetDefaultTexture(filename);

		Error err = image.Load(path);

        if (err != Error.Ok)
            return null;

        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D GetDefaultTexture(string filename)
        => GD.Load<Texture2D>($"res://assets/defaultskin/{filename}");
}