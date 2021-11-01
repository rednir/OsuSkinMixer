using System.Collections.Generic;

namespace OsuSkinMixer
{
    public class SkinIniSection
    {
        public string Name { get; set; }

        public Dictionary<string, string> KeyValuePairs { get; set; } = new Dictionary<string, string>();
    }
}