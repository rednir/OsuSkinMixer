namespace OsuSkinMixer.Models;

using System.Text;

/// <summary>Represents a section in a osu! skin's skin.ini file, such as "[General]", with its values represented as key-value pairs.</summary>
public class OsuSkinIniSection : Dictionary<string, string>
{
    public string Name { get; set; }

    public OsuSkinIniSection(string name)
    {
        Name = name;
    }

    /// <summary>Converts this section to a string in the skin.ini format.</summary>
    /// <returns>A string in the skin.ini format.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(Name).AppendLine("]");
        foreach (var pair in this)
            sb.Append(pair.Key).Append(": ").AppendLine(pair.Value);

        return sb.ToString();
    }
}