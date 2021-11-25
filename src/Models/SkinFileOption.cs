namespace OsuSkinMixer
{
    public class SkinFileOption : SkinOption
    {
        public SkinFileOption(string fileName, bool isAudio)
        {
            IncludeFileName = fileName;
            IsAudio = isAudio;
        }

        public override string Name => IncludeFileName + (IsAudio ? ".wav" : ".png");

        public string IncludeFileName { get; set; }

        public bool IsAudio { get; set; }

        public override string ToString() => $"{(IsAudio ? "Audio" : "Image")}:  '{IncludeFileName}'";
    }
}