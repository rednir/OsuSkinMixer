namespace OsuSkinMixer
{
    public class SkinIniOption : SkinOption
    {
        public SkinIniOption(string section, string property)
        {
            IncludeSkinIniProperty = (section, property);
        }

        public override string Name => IncludeSkinIniProperty.property;

        public (string section, string property) IncludeSkinIniProperty { get; set; }

        public override string ToString() => $"[{IncludeSkinIniProperty.section}]\n{IncludeSkinIniProperty.property}: ???";
    }
}