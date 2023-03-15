using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using OsuSkinMixer.Statics;
using File = System.IO.File;

namespace OsuSkinMixer.Models;

/// <summary>Represents an osu! skin and provides methods to fetch its elements.</summary>
public class OsuSkin
{
    public static Color[] DefaultComboColors
        => new Color[]
        {
            new Color(1, 0.7529f, 0),
            new Color(0, 0.7922f, 0),
            new Color(0.0706f, 0.4863f, 1),
            new Color(0.9490f, 0.0941f, 0.2235f),
        };

    public OsuSkin(string name, DirectoryInfo dir, bool hidden = false)
    {
        Name = name;
        Directory = dir;
        SkinIni = new OsuSkinIni(name, "osu! skin mixer by rednir");
        Hidden = hidden;
    }

    public OsuSkin(DirectoryInfo dir, bool hidden = false)
    {
        Name = dir.Name;
        Directory = dir;
        Hidden = hidden;
        if (File.Exists($"{dir.FullName}/skin.ini"))
        {
            try
            {
                SkinIni = new OsuSkinIni(File.ReadAllText($"{dir.FullName}/skin.ini"));
            }
            catch (Exception ex)
            {
                Settings.PushException(new InvalidOperationException($"Failed to load {dir.FullName}/skin.ini", ex));
            }
        }
    }

    public string Name { get; set; }

    public DirectoryInfo Directory { get; set; }

    public OsuSkinIni SkinIni { get; set; }

    public bool Hidden { get; set; }

    public Color[] ComboColors
    {
        get
        {
            OsuSkinIniSection colorsSection = SkinIni?
                .Sections
                .Find(x => x.Name == "Colours");

            if (colorsSection == null)
                return DefaultComboColors;

            List<Color> comboColorList = new();

            for (int i = 1; i <= 8; i++)
            {
                string[] rgb = colorsSection
                    .GetValueOrDefault($"Combo{i}")?
                    .Replace(" ", string.Empty)
                    .Split(',');

                // Break if no more colors defined in skin.ini.
                if (rgb == null)
                    break;

                if (float.TryParse(rgb[0], out float r)
                    && float.TryParse(rgb[1], out float g)
                    && float.TryParse(rgb[2], out float b))
                {
                    comboColorList.Add(new Color(r / 255, g / 255, b / 255));
                }
                else
                {
                    // TODO: what does osu! do?
                }
            }

            if (comboColorList.Count == 0)
                return DefaultComboColors;

            return comboColorList.ToArray();
        }
    }

    public override string ToString()
        => Name;

    public override bool Equals(object obj)
        => obj is OsuSkin skin && Name == skin?.Name;

    public override int GetHashCode()
        => Name.GetHashCode();

    public Texture2D GetTexture(string filename, string extension = "png", bool fallbackToNull = false)
    {
        if (_textureCache.TryGetValue(filename, out Texture2D value))
            return value;

        Settings.Log($"Loading texture '{filename}' for skin: {Name}");

        string path = $"{Directory.FullName}/{filename}.{extension}";

        // Often default-x.png is found in a subdirectory defined by the skin.ini, so check for that.
        if (filename.StartsWith("default-", StringComparison.OrdinalIgnoreCase))
        {
            string hitCirclePrefix = SkinIni?.TryGetPropertyValue("Fonts", "HitCirclePrefix");

            if (hitCirclePrefix != null)
                path = hitCirclePrefix != null ? $"{Directory.FullName}/{hitCirclePrefix}{filename[7..]}.{extension}" : $"{Directory.FullName}/default";
        }

        if (!File.Exists(path))
        {
            if (fallbackToNull)
                return null;

            Settings.Log("Falling back to default texture");
            var defaultTexture = GetDefaultTexture($"{filename}.{extension}");
            _textureCache.Add(filename, defaultTexture);
            return defaultTexture;
        }

        Image image = new();
        Error err = image.Load(path);

        if (err != Error.Ok)
            return null;

        var texture = ImageTexture.CreateFromImage(image);
        _textureCache.Add(filename, texture);
        return texture;
    }

    public SpriteFrames GetSpriteFrames(params string[] filenames)
    {
        SpriteFrames spriteFrames = new();

        if (!int.TryParse(SkinIni?.TryGetPropertyValue("General", "AnimationFramerate"), out int fps))
            fps = -1;

        foreach (string filename in filenames)
        {
            string pathPrefix = $"{Directory.FullName}/{filename}";

            spriteFrames.AddAnimation(filename);

            for (int i = 0; File.Exists($"{pathPrefix}-{i}.png"); i++)
                spriteFrames.AddFrame(filename, GetTexture($"{filename}-{i}"));

            // AnimationFramerate of the default value -1 makes osu! play all the frames in 1 second.
            spriteFrames.SetAnimationSpeed(filename, fps != -1 ? fps : spriteFrames.GetFrameCount(filename));
            spriteFrames.SetAnimationLoop(filename, false);

            if (spriteFrames.GetFrameCount(filename) == 0)
                spriteFrames.AddFrame(filename, GetTexture($"{filename}"));
        }

        return spriteFrames;
    }

    public void ClearTextureCache()
        => _textureCache.Clear();

    private readonly Dictionary<string, Texture2D> _textureCache = new();

    private static Texture2D GetDefaultTexture(string filenameWithExtension)
        => GD.Load<Texture2D>($"res://assets/defaultskin/{filenameWithExtension}");
}