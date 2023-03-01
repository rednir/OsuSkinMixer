using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using File = System.IO.File;

namespace OsuSkinMixer.Models.Osu;

public class OsuSkin
{
    public OsuSkin()
    {
    }

    public OsuSkin(DirectoryInfo dir)
    {
        Name = dir.Name;
        Directory = dir;
        if (File.Exists($"{dir.FullName}/skin.ini"))
        {
            try
            {
                SkinIni = new OsuSkinIni(File.ReadAllText($"{dir.FullName}/skin.ini"));
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

    public OsuSkinIni SkinIni { get; set; }

    public Texture2D GetTexture(string filename)
    {
        if (_textureCache.TryGetValue(filename, out Texture2D value))
            return value;

        GD.Print($"Loading texture {filename} for skin: {Name}");

        string path = $"{Directory.FullName}/{filename}";
		Image image = new();

        if (!File.Exists(path))
        {
            GD.Print("Falling back to default texture.");
            var defaultTexture = GetDefaultTexture(filename);
            _textureCache.Add(filename, defaultTexture);
            return GetDefaultTexture(filename);
        }

		Error err = image.Load(path);

        if (err != Error.Ok)
            return null;

        var texture = ImageTexture.CreateFromImage(image);
        _textureCache.Add(filename, texture);
        return texture;
    }

    private readonly Dictionary<string, Texture2D> _textureCache = new();

    private static Texture2D GetDefaultTexture(string filename)
        => GD.Load<Texture2D>($"res://assets/defaultskin/{filename}");
}