namespace OsuSkinMixer.Models;

/// <summary>Represents an option with the target of an image or audio file, or several.</summary>
public class SkinFileOption : SkinOption
{
    public SkinFileOption(string fileName, bool isAudio, string displayName = null, bool isAnimatable = false)
    {
        IncludeFileName = fileName;
        IsAudio = isAudio;
        DisplayName = displayName;
        IsAnimatable = isAnimatable;
    }

    public override string Name => DisplayName ?? IncludeFileName + (IsAudio ? ".wav" : ".png");

    public string IncludeFileName { get; set; }

    public bool IsAudio { get; set; }

    public string DisplayName { get; set; }

    public bool IsAnimatable { get; set; }

    public override string ToString() => $"{(IsAudio ? "Audio" : "Image")}:  '{IncludeFileName}'";
}