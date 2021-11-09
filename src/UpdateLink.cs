using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OsuSkinMixer
{
    public class UpdateLink : LinkButton
    {
        public override void _Ready()
        {
            Connect("pressed", this, "_UpdateLinkPressed");
            CheckForUpdates();
        }

        private void _UpdateLinkPressed()
        {
            OS.ShellOpen("https://github.com/rednir/OsuSkinMixer/releases/latest");
        }

        private void CheckForUpdates()
        {
            var req = GetNode<HTTPRequest>("HTTPRequest");
            req.Connect("request_completed", this, "_UpdateRequestCompleted");
            req.Request("https://api.github.com/repos/rednir/OsuSkinMixer/releases/latest", new string[] { "User-Agent: OsuSkinMixer" });
        }

        private void _UpdateRequestCompleted(int result, int response_code, string[] headers, byte[] body)
        {
            if (result == 0)
            {
                try
                {
                    string latest = JsonSerializer.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(body))["tag_name"].ToString();
                    if (latest != Settings.VERSION)
                    {
                        Text = $"Updates are available! ({Settings.VERSION} -> {latest})";
                        GetNode<AnimationPlayer>("AnimationPlayer").Play("update");
                        return;
                    }

                    Text = $"{Settings.VERSION} (latest)";
                    return;
                }
                catch
                {
                }
            }

            Text = $"{Settings.VERSION} (unknown)";
        }
    }
}