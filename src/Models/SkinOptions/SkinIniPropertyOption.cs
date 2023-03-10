namespace OsuSkinMixer.Models;

/// <summary>Represents an option with the target of a single skin.ini property.</summary>
public class SkinIniPropertyOption : SkinOption
{
    public SkinIniPropertyOption(string section, string property)
    {
        IncludeSkinIniProperty = (section, property);
    }

    public override string Name => $"[skin.ini] {IncludeSkinIniProperty.property}";

    public (string section, string property) IncludeSkinIniProperty { get; set; }

    public override string ToString() => $"[{IncludeSkinIniProperty.section}]\n{IncludeSkinIniProperty.property}:";
}