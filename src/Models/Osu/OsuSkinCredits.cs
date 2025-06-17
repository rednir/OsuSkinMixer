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

        foreach (string line in lines)
        {
            throw new NotImplementedException();
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
                sb.Append(item).Append(", ");
            }

            // Remove the last comma and space.
            sb.Length -= 2;
            sb.AppendLine().AppendLine();
        }

        return sb.ToString();
    }
}