namespace OsuSkinMixer.Models;

using System.IO;
using OsuSkinMixer.Statics;

/// <summary>Represents an osu! skin and provides methods to fetch its elements.</summary>
public class OsuSkin
{
    private readonly object _lock = new();

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
        LoadSkinIni();
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

    public Texture2D Get2XTexture(string filename, string extension = "png")
    {
        lock (_lock)
        {
            TryGet2XTexture(filename, out Texture2D result, extension);
            return result;
        }
    }

    public Texture2D GetTexture(string filename, string extension = "png")
    {
        lock (_lock)
        {
            TryGetTexture(filename, out Texture2D result, extension);
            return result;
        }
    }

    public bool TryGet2XTexture(string filename, out Texture2D result, string extension = "png")
    {
        lock (_lock)
        {
            result = GetTextureOrNull($"{filename}@2x", extension);

            if (result != null)
                return true;

            result = GetTextureOrNull(filename, extension);

            if (result != null)
                return false;

            result = GetDefaultTexture($"{filename}@2x.{extension}");
            return true;
        }
    }

    public bool TryGetTexture(string filename, out Texture2D result, string extension = "png")
    {
        lock (_lock)
        {
            result = GetTextureOrNull(filename, extension);

            if (result != null)
                return true;

            result = GetDefaultTexture($"{filename}.{extension}");
            return false;
        }
    }

    private Texture2D GetTextureOrNull(string filename, string extension)
    {
        if (_textureCache.TryGetValue(filename, out Texture2D value))
            return value;

        string path = $"{Directory.FullName}/{filename}.{extension}";

        if (!File.Exists(path))
        {
            _textureCache.TryAdd(filename, null);
            return null;
        }

        Image image = new();
        Error err = image.Load(path);

        if (err != Error.Ok)
        {
            _textureCache.TryAdd(filename, null);
            return null;
        }

        var texture = ImageTexture.CreateFromImage(image);
        _textureCache.TryAdd(filename, texture);
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

            for (int i = 0;; i++)
            {
                if (File.Exists($"{pathPrefix}-{i}@2x.png") || File.Exists($"{pathPrefix}-{i}.png"))
                {
                    TryGet2XTexture($"{filename}-{i}", out var texture);
                    spriteFrames.AddFrame(filename, texture);
                    continue;
                }

                break;
            }

            // AnimationFramerate of the default value -1 makes osu! play all the frames in 1 second.
            spriteFrames.SetAnimationSpeed(filename, fps != -1 ? fps : spriteFrames.GetFrameCount(filename));
            spriteFrames.SetAnimationLoop(filename, false);

            if (spriteFrames.GetFrameCount(filename) == 0)
            {
                TryGet2XTexture(filename, out var texture);
                spriteFrames.AddFrame(filename, texture);
            }
        }

        return spriteFrames;
    }

    public AudioStream GetAudioStream(string filename)
    {
        string pathPrefix = $"{Directory.FullName}/{filename}";

        if (File.Exists(pathPrefix + ".wav"))
        {
            return Tools.GetAudioStreamWav(File.ReadAllBytes(pathPrefix + ".wav"));
        }
        else if (File.Exists(pathPrefix + ".ogg"))
        {
            return AudioStreamOggVorbis.LoadFromFile(pathPrefix + ".ogg");
        }

        return null;
    }

    public void ClearCache()
    {
        _textureCache.Clear();
        Directory.Refresh();
        LoadSkinIni();
    }

    private void LoadSkinIni()
    {
        if (File.Exists($"{Directory.FullName}/skin.ini"))
        {
            try
            {
                SkinIni = new OsuSkinIni(File.ReadAllText($"{Directory.FullName}/skin.ini"));
            }
            catch (Exception ex)
            {
                Settings.PushException(new InvalidOperationException($"Failed to load {Directory.FullName}/skin.ini", ex));
            }
        }
        else
        {
            SkinIni = new OsuSkinIni(Name, "unknown");
        }
    }

    private readonly ConcurrentDictionary<string, Texture2D> _textureCache = new();

    private static Texture2D GetDefaultTexture(string filenameWithExtension)
        => GD.Load<Texture2D>($"res://assets/defaultskin/{filenameWithExtension}");
}