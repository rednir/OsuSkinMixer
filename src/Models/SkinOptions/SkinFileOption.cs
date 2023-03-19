namespace OsuSkinMixer.Models;

/// <summary>Represents an option with the target of an image or audio file, or several.</summary>
public class SkinFileOption : SkinOption
{
    public SkinFileOption(string fileName, bool isAudio, string displayName = null, bool isAnimatable = false, string[] allowedSuffixes = null)
    {
        IncludeFileName = fileName;
        IsAudio = isAudio;
        DisplayName = displayName;
        IsAnimatable = isAnimatable;
        AllowedSuffixes = allowedSuffixes;
    }

    public override string Name => DisplayName ?? IncludeFileName + (IsAudio ? ".wav" : ".png");

    public string IncludeFileName { get; set; }

    public bool IsAudio { get; set; }

    public string DisplayName { get; set; }

    public bool IsAnimatable { get; set; }

    /// <summary>Represents the allowed suffixes for the file, ignored if the include filename does not end with a wildcard. If null, all prefixes are allowed.</summary>
    public string[] AllowedSuffixes { get; set; }

    public override string ToString() => $"{(IsAudio ? "Audio" : "Image")}:  '{IncludeFileName}'";
}