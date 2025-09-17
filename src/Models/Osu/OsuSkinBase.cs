using System;
using System.IO;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Models.Osu;

public abstract class OsuSkinBase
{
    public const string DEFAULT_AUTHOR = "osu! skin mixer by rednir";

    public static Color[] DefaultComboColors =>
    [
        new Color(1, 0.7529f, 0),
        new Color(0, 0.7922f, 0),
        new Color(0.0706f, 0.4863f, 1),
        new Color(0.9490f, 0.0941f, 0.2235f),
    ];

    public string Name { get; protected set; }

    public OsuSkinIni SkinIni { get; protected set; }

    protected OsuSkinCredits _credits;

    public abstract OsuSkinCredits Credits { get; }

    public bool Hidden { get; protected set; }

    // TODO: always top dir only?
    public IEnumerable<OsuSkinFile> Files => EnumerateFiles(SearchOption.AllDirectories);

    public abstract OsuSkinFile TryGetFile(string virtualPath);

    public bool ContainsFile(string virtualPath)
        => TryGetFile(virtualPath) is not null;

    public Color[] ComboColors
    {
        get
        {
            OsuSkinIniSection colorsSection = SkinIni?
                .Sections
                .Find(x => x.Name == "Colours");

            if (colorsSection == null)
                return DefaultComboColors;

            List<Color> comboColorList = [];

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
        => obj is OsuSkinBase skin && Name == skin?.Name;

    public override int GetHashCode()
        => Name.GetHashCode();

    protected abstract IEnumerable<OsuSkinFile> EnumerateFiles(SearchOption searchOption);

    public string GetElementFilepathWithoutExtension(string elementName)
    {
        // TODO: lazer
        return "slkdjfklasdjf";
    }

    public string WriteCreditsFile()
    {
        string destination = "credits.ini";
        OsuData.WriteFileToSkin(this, destination, Credits.ToString());
        return destination;
    }

    protected void LoadSkinIni(params string[] filePaths)
    {
        if (filePaths.Length == 0)
            throw new ArgumentException("No skin.ini file paths provided, cannot load.");

        foreach (string filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    SkinIni = new OsuSkinIni(File.ReadAllText(filePath));
                    return;
                }
                catch (Exception ex)
                {
                    Settings.PushException(new InvalidOperationException($"Failed to load skin.ini at {filePath}", ex));
                }
            }
        }

        SkinIni = new OsuSkinIni(Name, "unknown");
    }

    protected void LoadCreditsFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                _credits = new OsuSkinCredits(File.ReadAllText(filePath));
            }
            else
            {
                _credits = new OsuSkinCredits();
            }
        }
        catch (Exception ex)
        {
            Settings.PushException(new InvalidOperationException($"Couldn't load incorrectly formatted skin credits file: {filePath}", ex));
            _credits = new OsuSkinCredits();
        }
    }
}
