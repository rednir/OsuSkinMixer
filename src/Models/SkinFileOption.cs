namespace OsuSkinMixer
{
    public class SkinFileOption : SkinOption
    {
        public SkinFileOption(string fileName, bool isAudio)
        {
            IncludeFileName = fileName;
            IsAudio = isAudio;
        }

        public override string Name => IncludeFileName;

        public string IncludeFileName { get; set; }

        public bool IsAudio { get; set; }
    }
}