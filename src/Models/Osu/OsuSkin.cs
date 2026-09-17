namespace OsuSkinMixer.Models;

using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using OsuSkinMixer.Autoload;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Storage;

/// <summary>Represents an osu! skin and provides methods to fetch its elements.</summary>
public class OsuSkin
{
    public const string DEFAULT_AUTHOR = "osu! skin mixer by rednir";

    private readonly object _lock = new();
    private readonly string transientId = Guid.NewGuid().ToString();
    private DirectoryInfo directory;
    private SkinWorkspace readWorkspace;
    private SkinWorkspace previewWorkspace;
    public ISkinLibrary Library { get; private set; }
    public SkinRecord Record { get; private set; }
    public string Identity => Record == null ? transientId : SkinPaths.Identity(Library.Root) + ":" + Record.Id;
    public bool IsLazer => Library?.Kind == OsuClientKind.Lazer;
    public bool CanEdit => Record == null || (Record.IsLegacy && Record.Problem == null && Library.WriteRestriction == null);
    public bool CanExport => Record?.Problem == null;
    public DateTime Modified => Record?.Modified ?? Directory?.LastWriteTime ?? DateTime.MinValue;

    public OsuSkin(ISkinLibrary library, SkinRecord record)
    {
        Library = library;
        UpdateRecord(record);
    }

    public void UpdateRecord(SkinRecord record)
    {
        Record = record;
        Name = record.Name;
        Hidden = record.Hidden;
        // Keep old preview files valid for in-flight texture/audio loads until shutdown.
        readWorkspace = null;
        previewWorkspace = null;
        directory = null;
        _textureCache.Clear();
        _credits = null;
        LoadSkinIni();
    }

    public SkinWorkspace CreateWorkspace() => Library != null ? Library.Materialize(Record) : SkinWorkspace.Copy(SkinPaths.Enumerate(Directory.FullName));
    public SkinSnapshot DeleteFromDisk() => Library.Delete(Record);
    public void Restore(SkinSnapshot snapshot) => UpdateRecord(Library.Restore(snapshot));
    public void ReplaceFromWorkspace(SkinWorkspace workspace) => UpdateRecord(Library.Replace(Record, workspace));
    public OsuSkin Duplicate(string name) => new(Library, Library.Duplicate(Record, name));
    public InstallResult DuplicateWithResult(string name, bool overwriteExisting = false) => Library.DuplicateWithResult(Record, name, overwriteExisting);
    public void Rename(string name) => UpdateRecord(Library.Rename(Record, name));
    public void SetHidden(bool hidden) => UpdateRecord(Library.SetHidden(Record, hidden));
    public void Export(string path)
    {
        if (Library != null) Library.Export(Record, path);
        else { using var workspace = CreateWorkspace(); workspace.Export(path); }
    }
    public string FindFile(string filename)
    {
        if (Record != null) return Record.Files.TryGetValue(filename, out var path) && File.Exists(path) ? path : null;
        return Directory == null ? null : SkinPaths.Enumerate(Directory.FullName).GetValueOrDefault(filename);
    }

    public static Color[] DefaultComboColors
        => new Color[]
        {
            new Color(1, 0.7529f, 0),
            new Color(0, 0.7922f, 0),
            new Color(0.0706f, 0.4863f, 1),
            new Color(0.9490f, 0.0941f, 0.2235f),
        };

    private static readonly string[] DefaultAudioFiles =
    [
        "applause.wav",
        "check-off.wav",
        "check-on.wav",
        "click-close.wav",
        "click-short-confirm.wav",
        "click-short.wav",
        "combobreak.wav",
        "count.wav",
        "count1s.wav",
        "count2s.wav",
        "count3s.wav",
        "drum-hitclap.wav",
        "drum-hitfinish.wav",
        "drum-hitnormal.wav",
        "drum-hitwhistle.wav",
        "drum-sliderslide.wav",
        "drum-slidertick.wav",
        "drum-sliderwhistle.wav",
        "failsound.mp3",
        "gos.wav",
        "heartbeat.wav",
        "key-confirm.wav",
        "key-delete.wav",
        "key-movement.wav",
        "key-press-1.wav",
        "key-press-2.wav",
        "key-press-3.wav",
        "key-press-4.wav",
        "match-confirm.wav",
        "match-join.wav",
        "match-leave.wav",
        "match-notready.wav",
        "match-ready.wav",
        "match-start.wav",
        "menu-back-click.wav",
        "menu-charts-click.wav",
        "menu-direct-click.wav",
        "menu-edit-hover.mp3",
        "menu-exit-click.wav",
        "menu-freeplay-click.wav",
        "menu-multiplayer-click.wav",
        "menu-options-click.wav",
        "menu-play-click.wav",
        "menuback.wav",
        "menuclick.wav",
        "menuhit.wav",
        "metronomelow.wav",
        "nightcore-clap.wav",
        "nightcore-finish.wav",
        "nightcore-hat.wav",
        "nightcore-kick.wav",
        "normal-hitclap.wav",
        "normal-hitfinish.wav",
        "normal-hitnormal.wav",
        "normal-hitwhistle.wav",
        "normal-sliderslide.wav",
        "normal-slidertick.wav",
        "normal-sliderwhistle.wav",
        "pause-back-click.wav",
        "pause-continue-click.wav",
        "pause-hover.wav",
        "pause-loop.mp3",
        "pause-retry-click.wav",
        "readys.wav",
        "sectionfail.wav",
        "sectionpass.wav",
        "seeya.wav",
        "select-difficulty.wav",
        "select-expand.wav",
        "shutter.wav",
        "sliderbar.wav",
        "soft-hitclap.wav",
        "soft-hitfinish.wav",
        "soft-hitnormal.wav",
        "soft-hitwhistle.wav",
        "soft-sliderslide.wav",
        "soft-slidertick.wav",
        "soft-sliderwhistle.wav",
        "spinnerbonus.wav",
        "spinnerspin.wav",
        "taiko-normal-hitclap.wav",
        "taiko-normal-hitfinish.wav",
        "taiko-normal-hitnormal.wav",
        "taiko-normal-hitwhistle.wav",
        "taiko-soft-hitclap.wav",
        "taiko-soft-hitfinish.wav",
        "taiko-soft-hitnormal.wav",
        "taiko-soft-hitwhistle.wav",
        "welcome.wav",
    ];

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

    /// <summary>Legacy image-engine view. For lazer this is an app-owned copy, never the blob store.</summary>
    public DirectoryInfo Directory
    {
        get
        {
            if (directory == null && Record != null)
            {
                if (!IsLazer) directory = new DirectoryInfo(Record.Id);
                else
                {
                    readWorkspace ??= Library.Materialize(Record, allowDegraded: true);
                    directory = new DirectoryInfo(readWorkspace.DirectoryPath);
                }
            }
            return directory;
        }
        set => directory = value;
    }

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
            if (Record != null)
                return Record.Files.Keys.Count(f => new[] { ".png", ".jpg", ".wav", ".ogg", ".mp3" }.Contains(Path.GetExtension(f).ToLowerInvariant()));
            if (Directory is null)
                return 0;

            string[] extensions = [".png", ".jpg", ".wav", ".ogg", ".mp3"];
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
        => obj is OsuSkin skin && Identity == skin.Identity;

    public override int GetHashCode()
        => Identity.GetHashCode();

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
    {
        SkinPaths.Virtual(filename + "__prefix");
        if (Record == null || !IsLazer) return Path.Combine(Directory?.FullName ?? string.Empty, filename);
        lock (_lock)
        {
            previewWorkspace ??= new SkinWorkspace();
            var prefix = Path.Combine(previewWorkspace.DirectoryPath, filename);
            foreach (var suffix in new[] { ".png", "@2x.png", ".jpg", "@2x.jpg" })
            {
                var source = FindFile(filename + suffix);
                if (source == null || File.Exists(prefix + suffix)) continue;
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(prefix));
                File.Copy(source, prefix + suffix);
            }
            return prefix;
        }
    }

    private Texture2D GetTextureOrNull(string filename, string extension)
    {
        string cacheKey = filename + "." + extension;
        if (_textureCache.TryGetValue(cacheKey, out Texture2D value))
            return value;

        string path = FindFile($"{filename}.{extension}");

        if (!File.Exists(path))
        {
            _textureCache.TryAdd(cacheKey, null);
            return null;
        }

        using Image image = new();
        // Lazer's physical blob path has no image extension; decode using the virtual filename's format.
        var bytes = File.ReadAllBytes(path);
        Error err = extension.Equals("jpg", StringComparison.OrdinalIgnoreCase) ? image.LoadJpgFromBuffer(bytes) : image.LoadPngFromBuffer(bytes);

        if (err != Error.Ok)
        {
            _textureCache.TryAdd(cacheKey, null);
            return null;
        }

        var texture = ImageTexture.CreateFromImage(image);
        _textureCache.TryAdd(cacheKey, texture);
        return texture;
    }

    public void AddSpriteFramesAnimation(SpriteFrames spriteFrames, string filename, bool use2x)
    {
        if (!int.TryParse(SkinIni?.TryGetPropertyValue("General", "AnimationFramerate"), out int fps))
            fps = -1;

        spriteFrames.AddAnimation(filename);

        for (int i = 0; ; i++)
        {
            if (FindFile($"{filename}-{i}@2x.png") != null || FindFile($"{filename}-{i}.png") != null)
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
        spriteFrames.SetAnimationLoopMode(filename, SpriteFrames.LoopMode.None);

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
        if (string.IsNullOrEmpty(filename)) return null;
        try
        {
            foreach (var extension in new[] { ".wav", ".ogg", ".mp3" })
            {
                string path;
                if (filename.EndsWith('*'))
                {
                    var prefix = filename.TrimEnd('*');
                    var files = Record?.Files ?? SkinPaths.Enumerate(Directory.FullName);
                    path = files.Where(p => p.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && p.Key.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => p.Value).FirstOrDefault(File.Exists);
                }
                else path = FindFile(filename + extension);
                if (path == null) continue;
                return extension switch
                {
                    ".wav" => AudioStreamWav.LoadFromFile(path),
                    ".ogg" => AudioStreamOggVorbis.LoadFromFile(path),
                    _ => AudioStreamMP3.LoadFromFile(path),
                };
            }
        }
        catch (Exception e) { Settings.Log($"Audio fallback: {e.Message}"); }
        return GetDefaultAudioStream(filename);
    }

    public void ClearCache()
    {
        _textureCache.Clear();
        _credits = null;
        directory?.Refresh();
        LoadSkinIni();
    }

    private void LoadSkinIni()
    {
        var iniPath = FindFile("skin.ini");
        if (iniPath != null)
        {
            try { SkinIni = new OsuSkinIni(File.ReadAllText(iniPath)); }
            catch (Exception e)
            {
                SkinIni = new OsuSkinIni(Name, Record?.Author ?? "unknown");
                if (Record != null) Record = Record with { Problem = "The skin.ini could not be read. Repair it before editing this skin." };
                Settings.Log($"INI fallback for {Name}: {e.Message}");
            }
            return;
        }
        if (Record != null)
        {
            SkinIni = new OsuSkinIni(Name, Record.Author);
            return;
        }
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
            string creditsPath = FindFile(OsuSkinCredits.FILE_NAME);

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
        => ResourceLoader.Exists($"res://assets/defaultskin/{filenameWithExtension}") ? GD.Load<T>($"res://assets/defaultskin/{filenameWithExtension}") : null;

    private static AudioStream GetDefaultAudioStream(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return null;

        if (filename.EndsWith('*'))
        {
            string prefix = filename.TrimEnd('*');
            string match = DefaultAudioFiles
                .Where(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return match != null ? GetDefaultElement<AudioStream>(match) : null;
        }

        if (Path.HasExtension(filename))
        {
            string match = DefaultAudioFiles
                .FirstOrDefault(file => file.Equals(filename, StringComparison.OrdinalIgnoreCase));

            return match != null ? GetDefaultElement<AudioStream>(match) : null;
        }

        string baseMatch = DefaultAudioFiles
            .Where(file => Path.GetFileNameWithoutExtension(file)
                .Equals(filename, StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (baseMatch != null)
            return GetDefaultElement<AudioStream>(baseMatch);

        return GetDefaultElement<AudioStream>($"{filename}.wav");
    }
}
