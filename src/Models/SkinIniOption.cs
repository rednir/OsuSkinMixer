namespace OsuSkinMixer
{
    public class SkinIniOption : SkinOption
    {
        public SkinIniOption(string section, string property)
        {
            IncludeSkinIniProperty = (section, property);
        }

        public override string Name => $"[{IncludeSkinIniProperty.section}] {IncludeSkinIniProperty.property}";

        public (string section, string property) IncludeSkinIniProperty { get; set; }
    }
}