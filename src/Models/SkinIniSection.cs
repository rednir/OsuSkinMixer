using System.Collections.Generic;

namespace OsuSkinMixer
{
    public class SkinIniSection : Dictionary<string, string>
    {
        public SkinIniSection(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}