namespace OsuSkinMixer.Models;

using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using OsuSkinMixer.Autoload;
using OsuSkinMixer.Statics;

/// <summary>Represents an osu! skin and provides methods to fetch its elements.</summary>
public class OsuSkin
{
    public const string DEFAULT_AUTHOR = "osu! skin mixer by rednir";

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
        SkinIni = new OsuSkinIni(name, DEFAULT_AUTHOR);
        Hidden = hidden;
    }

    public OsuSkin(DirectoryInfo dir, bool hidden = false)
    {
        Name = dir.Name;
        Directory = dir;
        Hidden = hidden;
        LoadSkinIni();
    }

    // Constructor for creating a dummy skin (hacky solution for creating skin credits entries).
    public OsuSkin(string name, string author)
    {
        Name = name;
        SkinIni = new OsuSkinIni(name, author);
    }

    public string Name { get; set; }

    public DirectoryInfo Directory { get; set; }

    public OsuSkinIni SkinIni { get; set; }

    private OsuSkinCredits _credits;

    public OsuSkinCredits Credits
    {
        get
        {
            if (_credits is null)
                LoadCreditsFile();

            return _credits;
        }
    }

    public int ElementCount
    {
        get
        {
            if (Directory is null)
                return 0;

            string[] extensions = [".png", ".jpg", ".wav", ".ogg"];
            return Directory.EnumerateFiles("*", SearchOption.AllDirectories)
                .Count(f => extensions.Any(ext => f.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)));
        }
    }

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

    public string WriteCreditsFile()
    {
        string destination = $"{Directory.FullName}/credits.ini";
        File.WriteAllText(destination, Credits.ToString());
        return destination;
    }

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

            result = GetDefaultElement<Texture2D>($"{filename}@2x.{extension}");
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

            result = GetDefaultElement<Texture2D>($"{filename}.{extension}");
            return false;
        }
    }

    public string GetElementFilepathWithoutExtension(string filename)
        => $"{Directory.FullName}/{filename}";

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

    public void AddSpriteFramesAnimation(SpriteFrames spriteFrames, string filename, bool use2x)
    {
        if (!int.TryParse(SkinIni?.TryGetPropertyValue("General", "AnimationFramerate"), out int fps))
            fps = -1;

        string pathPrefix = $"{Directory.FullName}/{filename}";

        spriteFrames.AddAnimation(filename);

        for (int i = 0; ; i++)
        {
            if (File.Exists($"{pathPrefix}-{i}@2x.png") || File.Exists($"{pathPrefix}-{i}.png"))
            {
                if (use2x)
                {
                    TryGet2XTexture($"{filename}-{i}", out var texture);
                    spriteFrames.AddFrame(filename, texture);
                }
                else
                {
                    TryGetTexture($"{filename}-{i}", out var texture);
                    spriteFrames.AddFrame(filename, texture);
                }
                continue;
            }

            break;
        }

        // AnimationFramerate of the default value -1 makes osu! play all the frames in 1 second.
        spriteFrames.SetAnimationSpeed(filename, fps != -1 ? fps : spriteFrames.GetFrameCount(filename));
        spriteFrames.SetAnimationLoop(filename, false);

        if (spriteFrames.GetFrameCount(filename) == 0)
        {
            if (use2x)
            {
                TryGet2XTexture(filename, out var texture);
                spriteFrames.AddFrame(filename, texture);
            }
            else
            {
                TryGetTexture(filename, out var texture);
                spriteFrames.AddFrame(filename, texture);
            }
        }
    }

    public AudioStream GetAudioStream(string filename)
    {
        string pathPrefix = $"{Directory.FullName}/{filename}";

        try
        {
            if (File.Exists(pathPrefix + ".wav"))
            {
                return Tools.GetAudioStreamWav(File.ReadAllBytes(pathPrefix + ".wav"));
            }
            else if (File.Exists(pathPrefix + ".ogg"))
            {
                return AudioStreamOggVorbis.LoadFromFile(pathPrefix + ".ogg");
            }
        }
        catch
        {
            return null;
        }

        return GetDefaultElement<AudioStream>($"{filename}.wav");
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
        else if (File.Exists($"{Directory.FullName}/Skin.ini"))
        {
            // Hotfix for case-sensitive file systems.
            try
            {
                SkinIni = new OsuSkinIni(File.ReadAllText($"{Directory.FullName}/Skin.ini"));
            }
            catch (Exception ex)
            {
                Settings.PushException(new InvalidOperationException($"Failed to load {Directory.FullName}/Skin.ini", ex));
            }
        }
        else
        {
            SkinIni = new OsuSkinIni(Name, "unknown");
        }
    }

    private void LoadCreditsFile()
    {
        try
        {
            string creditsPath = $"{Directory.FullName}/{OsuSkinCredits.FILE_NAME}";

            if (File.Exists(creditsPath))
            {
                _credits = new OsuSkinCredits(File.ReadAllText(creditsPath));
            }
            else
            {
                _credits = new OsuSkinCredits();
            }
        }
        catch (Exception ex)
        {
            Settings.PushException(new InvalidOperationException($"Couldn't load incorrectly formatted skin credits file: {Directory.FullName}/{OsuSkinCredits.FILE_NAME}", ex));
            _credits = new OsuSkinCredits();
        }
    }

    private readonly ConcurrentDictionary<string, Texture2D> _textureCache = new();

    private static T GetDefaultElement<T>(string filenameWithExtension) where T : Resource
        => GD.Load<T>($"res://assets/defaultskin/{filenameWithExtension}");
}