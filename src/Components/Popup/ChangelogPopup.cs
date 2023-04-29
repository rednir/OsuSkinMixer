using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class ChangelogPopup : Popup
{
    public override void _Ready()
    {
        base._Ready();

        GetNode<Button>("%CloseButton").Pressed += Out;
        GetNode<Button>("%ViewChangelogButton").Pressed += () =>
            OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/releases/tag/{Settings.VERSION}");

        if (Settings.VERSION != Settings.Content.LastVersion)
        {
            if (Settings.Content.LastVersion != null)
                In();

            Settings.Content.LastVersion = Settings.VERSION;
            Settings.Save();
        }
    }
}
