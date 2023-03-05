using System.Collections.Generic;

namespace OsuSkinMixer.Models;

public class SkinOptionValue
{
    public SkinOptionValue(SkinOptionValueType type, OsuSkin customSkin = null)
    {
        Type = type;
        CustomSkin = customSkin;
    }

    public SkinOptionValueType Type { get; set; }

    public OsuSkin CustomSkin { get; set; }
}
