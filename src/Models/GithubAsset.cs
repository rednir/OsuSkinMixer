namespace OsuSkinMixer.Models;

using System.Text.Json.Serialization;

public class GithubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; }

    [JsonPropertyName("digest")]
    public string Digest { get; set; }
}