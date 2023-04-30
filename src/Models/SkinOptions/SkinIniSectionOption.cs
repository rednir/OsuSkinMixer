namespace OsuSkinMixer.Models;

/// <summary>Represents an option with the target of an entire skin.ini section which contains a matching <see cref="Property"/> key-pair value.</summary>
public class SkinIniSectionOption : SkinOption
{
    public SkinIniSectionOption(string section, string propertyName, string propertyValue)
    {
        SectionName = section;
        Property = new KeyValuePair<string, string>(propertyName, propertyValue);
    }

    public override string Name => $"[skin.ini] {Property.Key}: {Property.Value}";

    public string SectionName { get; set; }

    public KeyValuePair<string, string> Property { get; set; }

    public override string ToString() => $"Copy entire section where:\n\n[{SectionName}]\n{Property.Key}: {Property.Value}";
}