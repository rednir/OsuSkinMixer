namespace OsuSkinMixer.Models;

using System.Text.Json.Serialization;

public class GithubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }
}