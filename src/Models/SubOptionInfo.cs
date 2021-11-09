using System.Collections.Generic;

namespace OsuSkinMixer
{
    public class SubOptionInfo
    {
        public string Name { get; set; }

        public bool IsAudio { get; set; }

        public Dictionary<string, string[]> IncludeSkinIniProperties { get; set; } = new Dictionary<string, string[]>();

        public string[] IncludeFileNames { get; set; }

        public string GetPath(OptionInfo option) => $"{Main.SCROLL_CONTAINER_PATH}/{GetHBoxName(option)}/OptionButton";

        public string GetHBoxName(OptionInfo option) => $"{option.Name}_{Name}";
    }

}