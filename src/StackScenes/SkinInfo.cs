namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Statics;

public partial class SkinInfo : StackScene
{
    public override string Title => "Skin info";

    public IEnumerable<OsuSkinBase> Skins { get; set; }

    private PackedScene SkinInfoPanelScene;

    private PanelContainer DonateContainer;
    private Button DonateButton;
    private Button DismissButton;

    public override void _Ready()
    {
        SkinInfoPanelScene = GD.Load<PackedScene>("res://src/Components/SkinInfoPanel.tscn");
        DonateContainer = GetNode<PanelContainer>("%DonateContainer");
        DonateButton = GetNode<Button>("%DonateButton");
        DismissButton = GetNode<Button>("%DismissButton");

        DonateButton.Pressed += OnDonateButtonPressed;
        DismissButton.Pressed += OnDismissButtonPressed;

        if (Settings.Content.DonateLaunchCountThreshold == 0)
            Settings.Content.DonateLaunchCountThreshold = 3;

        DonateContainer.Visible = !Settings.Content.DonationMessageDismissed
            && Settings.Content.SkinsMadeCount >= 6
            && Settings.Content.LaunchCount >= Settings.Content.DonateLaunchCountThreshold;

        foreach (var skin in Skins)
        {
            var instance = SkinInfoPanelScene.Instantiate<SkinInfoPanel>();
            instance.Skin = skin;
            AddChild(instance);
        }
    }

    private void OnDonateButtonPressed()
    {
        OS.ShellOpen("https://github.com/rednir/rednir/blob/master/DONATE.md");
    }

    private void OnDismissButtonPressed()
    {
        DonateContainer.Visible = false;
        Settings.Content.DonationMessageDismissed = true;
    }
}
