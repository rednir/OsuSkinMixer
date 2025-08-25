namespace OsuSkinMixer.Statics;

using System.Text.Json.Serialization;
using OsuSkinMixer.Models;

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

        [JsonPropertyName("notify_on_skin_folder_change")]
        public bool NotifyOnSkinFolderChange { get; set; }

        [JsonPropertyName("volume")]
        public double Volume { get; set; } = -8;

        [JsonPropertyName("volume_mute")]
        public bool VolumeMute { get; set; }

        [JsonPropertyName("disable_effects")]
        public bool DisableEffects { get; set; }

        [JsonPropertyName("arrow_button_pressed")]
        public bool ArrowButtonPressed { get; set; }

        [JsonPropertyName("skins_made_count")]
        public int SkinsMadeCount { get; set; }

        [JsonPropertyName("launch_count")]
        public int LaunchCount { get; set; }

        [JsonPropertyName("donate_launch_count_threshold")]
        public int DonateLaunchCountThreshold { get; set; }

        [JsonPropertyName("donation_message_dismissed")]
        public bool DonationMessageDismissed { get; set; }

        [JsonPropertyName("operations")]
        [JsonIgnore]
        public List<Operation> Operations { get; set; } = new();

        [JsonPropertyName("skins_folder")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string SkinsFolder { get; set; }
    }
}