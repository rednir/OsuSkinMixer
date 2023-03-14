namespace OsuSkinMixer.Models;

/// <summary>Represents an option with the target of an image or audio file, or several.</summary>
public class SkinFileOption : SkinOption
{
    public SkinFileOption(string fileName, bool isAudio, string displayName = null)
    {
        DisplayName = displayName;
        IncludeFileName = fileName;
        IsAudio = isAudio;
    }

    public override string Name => DisplayName ?? IncludeFileName + (IsAudio ? ".wav" : ".png");

    public string DisplayName { get; set; }

    public string IncludeFileName { get; set; }

    public bool IsAudio { get; set; }

    public override string ToString() => $"{(IsAudio ? "Audio" : "Image")}:  '{IncludeFileName}'";
}