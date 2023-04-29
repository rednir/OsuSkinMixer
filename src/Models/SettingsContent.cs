using System.Text.Json.Serialization;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Statics;

public static partial class Settings
{
    public class SettingsContent
    {
        [JsonPropertyName("last_version")]
        public string LastVersion { get; set; }

        [JsonPropertyName("update_pending")]
        public bool UpdatePending { get; set; }

        [JsonPropertyName("osu_folder")]
        public string OsuFolder { get; set; }

        [JsonPropertyName("auto-update")]
        public bool AutoUpdate { get; set; }

        [JsonPropertyName("use_compact_skin_selector")]
        public bool UseCompactSkinSelector { get; set; }

        [JsonPropertyName("arrow_button_pressed")]
        public bool ArrowButtonPressed { get; set; }

        [JsonPropertyName("operations")]
        [JsonIgnore]
        public List<Operation> Operations { get; set; } = new();

        [JsonPropertyName("skins_folder")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string SkinsFolder { get; set; }
    }
}