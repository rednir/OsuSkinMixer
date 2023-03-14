using System.Text.Json.Serialization;

namespace OsuSkinMixer.Models;

public class GithubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }
}