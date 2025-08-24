using OsuSkinMixer.src.Models.Osu;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Models;

public class OsuSkinCredits
{
    public const string FILE_NAME = "credits.ini";

    public const string FILE_VERSION = "0.1";

    public const string FILE_GENERATED_BY = "osu! skin mixer";

    private readonly Dictionary<OsuSkinCreditsSkin, List<OsuSkinCreditsElement>> _credits = [];

    public OsuSkinCredits()
        : base()
    {
    }

    public OsuSkinCredits(string fileContent)
        : base()
    {
        string[] lines = fileContent.Split('\n');
        OsuSkinCreditsSkin currentSkin = null;

        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string line = lines[i].Trim();

            if (line.StartsWith("version:") && currentSkin is null)
            {
                // Ignore any newer versions, we don't know what the future holds.
                string version = line.Split(':', 2)[1].Trim();
                if (version != FILE_VERSION)
                {
                    Settings.Log($"Not parsing an incompatible credits file version: {version}");
                    return;
                }
            }

            // This is stricter detection than skin.ini, where the square brackets can have characters preceeding or following them.
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                string[] sectionNameSplit = line[1..^1].Split("\" by \"", StringSplitOptions.RemoveEmptyEntries);
                currentSkin = new OsuSkinCreditsSkin(
                    SkinName: sectionNameSplit[0].TrimStart('\"'),
                    SkinAuthor: sectionNameSplit[1].TrimEnd('\"'));

                // Failsafe in case there are duplicate section names, although this should never happen.
                if (!_credits.ContainsKey(currentSkin))
                    _credits[currentSkin] = [];

                continue;
            }

            if (currentSkin is null)
                continue;

            // Parse the element line.
            string[] elementParts = line.Split(" - ", 2, StringSplitOptions.RemoveEmptyEntries);

            _credits[currentSkin].Add(new OsuSkinCreditsElement(
                Checksum: elementParts[0].Trim(),
                Filename: elementParts[1].Trim()));
        }
    }

    public void AddElement(string skinName, string skinAuthor, string checksum, string filename)
    {
        if (!_credits.TryGetValue(new OsuSkinCreditsSkin(skinName, skinAuthor), out List<OsuSkinCreditsElement> values))
        {
            values = [];
            _credits[new OsuSkinCreditsSkin(skinName, skinAuthor)] = values;
        }

        values.Add(new OsuSkinCreditsElement(checksum, filename));
    }

    public void RemoveElement(string skinName, string skinAuthor, string filename)
    {
        if (_credits.TryGetValue(new OsuSkinCreditsSkin(skinName, skinAuthor), out List<OsuSkinCreditsElement> values))
        {
            values.RemoveAll(element => element.Filename == filename);

            // If there's nothing left to credit to this skin, remove it.
            if (values.Count <= 0)
                _credits.Remove(new OsuSkinCreditsSkin(skinName, skinAuthor));
        }
    }

    public bool TryGetElements(string skinName, string skinAuthor, out List<OsuSkinCreditsElement> value)
        => _credits.TryGetValue(new OsuSkinCreditsSkin(skinName, skinAuthor), out value);

    public bool TryGetSkinFromElementFilename(string filename, out OsuSkinCreditsSkin skin)
    {
        foreach (var pair in _credits)
        {
            if (pair.Value.Any(element => element.Filename == filename))
            {
                skin = pair.Key;
                return true;
            }
        }

        skin = null;
        return false;
    }

    public IEnumerable<KeyValuePair<OsuSkinCreditsSkin, List<OsuSkinCreditsElement>>> GetKeyValuePairs()
        => _credits;

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"version: {FILE_VERSION}")
          .AppendLine($"generated_by: {FILE_GENERATED_BY}")
          .AppendLine()
          .AppendLine();

        foreach (var pair in _credits)
        {
            sb.AppendLine($"[\"{pair.Key.SkinName}\" by \"{pair.Key.SkinAuthor}\"]");

            foreach (var item in pair.Value)
            {
                sb.Append(item.Checksum).Append(" - ").AppendLine(item.Filename);
            }

            sb.AppendLine()
              .AppendLine();
        }

        return sb.ToString();
    }
}