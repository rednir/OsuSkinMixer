namespace OsuSkinMixer.Models;

public class OsuSkinCredits : Dictionary<string, List<string>>
{
    public const string FILE_NAME = "credits.txt";

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

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var pair in this)
        {
            // TODO: author?
            sb.AppendLine($"[{pair.Key}]");

            foreach (var item in pair.Value)
            {
                sb.Append("CHECKSUMGOESHERE").Append(" - ").AppendLine(item);
            }

            // Remove the last comma and space.
            sb.Length -= 2;
            sb.AppendLine().AppendLine();
        }

        return sb.ToString();
    }
}