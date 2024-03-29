namespace OsuSkinMixer.Models;

/// <summary>Represents the value of a skin option set by the user.</summary>
public class SkinOptionValue
{
    public SkinOptionValue(SkinOptionValueType type)
    {
        if (type == SkinOptionValueType.CustomSkin)
            throw new System.ArgumentException("Custom skin not specified in constructor.");

        Type = type;
    }

    public SkinOptionValue(OsuSkin customSkin)
    {
        Type = SkinOptionValueType.CustomSkin;
        CustomSkin = customSkin;
    }

    public override string ToString()
    {
        return Type switch
        {
            SkinOptionValueType.CustomSkin => CustomSkin.Name,
            _ => Type.ToString(),
        };
    }

    public SkinOptionValueType Type { get; set; }

    public OsuSkin CustomSkin { get; set; }
}
