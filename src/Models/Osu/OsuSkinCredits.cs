using OsuSkinMixer.src.Models.Osu;

namespace OsuSkinMixer.Models;

public class OsuSkinCredits
{
    public const string FILE_NAME = "credits.ini";

    private readonly Dictionary<OsuSkinCreditsKey, List<OsuSkinCreditsValue>> _credits = [];

    public OsuSkinCredits()
        : base()
    {
    }

    public OsuSkinCredits(string fileContent)
        : base()
    {
        string[] lines = fileContent.Split('\n');
        string currentSkinName = null;

        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string line = lines[i].Trim();

            // This is stricter detection than skin.ini, where the square brackets can have characters preceeding or following them.
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                string[] sectionNameSplit = line[1..^1].Split("\" by \"", StringSplitOptions.RemoveEmptyEntries);

                currentSkinName = sectionNameSplit[0].TrimStart('\"');
                // TODO: author too!

                continue;
            }

            if (currentSkinName != null)
            {

            }
        }
    }

    public void Add(string skinName, string skinAuthor, string checksum, string filename)
    {
        if (!_credits.TryGetValue(new OsuSkinCreditsKey(skinName, skinAuthor), out List<OsuSkinCreditsValue> values))
        {
            values = [];
            _credits[new OsuSkinCreditsKey(skinName, skinAuthor)] = values;
        }

        values.Add(new OsuSkinCreditsValue(checksum, filename));
    }

    public bool TryGetValue(string skinName, string skinAuthor, out List<OsuSkinCreditsValue> value)
        => _credits.TryGetValue(new OsuSkinCreditsKey(skinName, skinAuthor), out value);

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var pair in _credits)
        {
            sb.AppendLine($"[\"{pair.Key.SkinName}\" by \"{pair.Key.SkinAuthor}\"]");

            foreach (var item in pair.Value)
            {
                sb.Append(item.Checksum).Append(" - ").AppendLine(item.FileName);
            }

            // Remove the last comma and space.
            sb.Length -= 2;
            sb.AppendLine().AppendLine();
        }

        return sb.ToString();
    }
}