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
                GD.PushError($"Failed to parse skin.ini for skin '{Name}' due to exception {ex.Message}");
                OS.Alert($"Skin.ini parse error for skin '{Name}', please report this error!\n\n{ex.Message}");
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

    public Texture2D GetTexture(string filename)
    {
        if (_textureCache.TryGetValue(filename, out Texture2D value))
            return value;

        Settings.Log($"Loading texture {filename} for skin: {Name}");

        string path = $"{Directory.FullName}/{filename}";

        if (!File.Exists(path))
        {
            Settings.Log("Falling back to default texture.");
            var defaultTexture = GetDefaultTexture(filename);
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

    public void ClearTextureCache()
        => _textureCache.Clear();

    private readonly Dictionary<string, Texture2D> _textureCache = new();

    private static Texture2D GetDefaultTexture(string filename)
        => GD.Load<Texture2D>($"res://assets/defaultskin/{filename}");
}