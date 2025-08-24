namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;

public partial class Menu : StackScene
{
    public override string Title => "Menu";

    private PackedScene SkinMixerScene;
    private PackedScene SkinModifierSkinSelectScene;
    private PackedScene SkinManagerScene;
    private PackedScene BeatmapSkinsScene;

    private Button SkinMixerButton;
    private Button SkinModifierButton;
    private Button SkinManagerButton;
    private Button BeatmapSkinsButton;
    private Button GetMoreSkinsButton;
    private Button LuckyButton;
    private TextureButton IconButton;
    private GetMoreSkinsPopup GetMoreSkinsPopup;

    private readonly Random _random = new();

    public override void _Ready()
    {
        SkinMixerScene = GD.Load<PackedScene>("res://src/StackScenes/SkinMixer.tscn");
        SkinModifierSkinSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierSkinSelect.tscn");
        SkinManagerScene = GD.Load<PackedScene>("res://src/StackScenes/SkinManager.tscn");
        BeatmapSkinsScene = GD.Load<PackedScene>("res://src/StackScenes/BeatmapSkinManager.tscn");

        SkinMixerButton = GetNode<Button>("%SkinMixerButton");
        SkinModifierButton = GetNode<Button>("%SkinModifierButton");
        SkinManagerButton = GetNode<Button>("%SkinManagerButton");
        BeatmapSkinsButton = GetNode<Button>("%BeatmapSkinsButton");
        GetMoreSkinsButton = GetNode<Button>("%GetMoreSkinsButton");
        IconButton = GetNode<TextureButton>("%IconButton");
        GetMoreSkinsPopup = GetNode<GetMoreSkinsPopup>("%GetMoreSkinsPopup");
        LuckyButton = GetNode<Button>("%LuckyButton");

        SkinMixerButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinMixerScene.Instantiate<StackScene>());
        SkinModifierButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinModifierSkinSelectScene.Instantiate<StackScene>());
        SkinManagerButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinManagerScene.Instantiate<StackScene>());
        BeatmapSkinsButton.Pressed += () => EmitSignal(SignalName.ScenePushed, BeatmapSkinsScene.Instantiate<StackScene>());
        GetMoreSkinsButton.Pressed += GetMoreSkinsPopup.In;
        LuckyButton.Pressed += OnLuckyButtonPressed;
        IconButton.Pressed += () => OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}");
    }

    private void OnLuckyButtonPressed()
    {
        if (OsuData.Skins.Length == 0)
        {
            EmitSignal(SignalName.ToastPushed, "You're out of luck!");
            return;
        }

        int randomIndex = _random.Next(0, OsuData.Skins.Length);
        OsuData.RequestSkinInfo(new[] { OsuData.Skins[randomIndex] });
    }
}
