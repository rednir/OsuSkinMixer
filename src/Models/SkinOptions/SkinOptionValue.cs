using System.Collections.Generic;

namespace OsuSkinMixer.Models;

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

    public SkinOptionValueType Type { get; set; }

    public OsuSkin CustomSkin { get; set; }
}
