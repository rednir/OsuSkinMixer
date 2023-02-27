namespace OsuSkinMixer.Models.SkinOptions;

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