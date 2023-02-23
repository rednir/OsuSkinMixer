using Godot;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Components;
using System.Collections.Generic;
using Skin = OsuSkinMixer.Models.Osu.Skin;
using System.Threading;

namespace OsuSkinMixer.StackScenes;

public partial class SkinMixer : StackScene
{
    public override string Title => "Skin Mixer";

    private CancellationTokenSource CancellationTokenSource;

    private PackedScene SkinInfoScene;

    private SkinCreatorPopup SkinCreatorPopup;
    private SkinOptionsSelector SkinOptionsSelector;
    private Button CreateSkinButton;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

        SkinCreatorPopup = GetNode<SkinCreatorPopup>("SkinCreatorPopup");
        SkinOptionsSelector = GetNode<SkinOptionsSelector>("SkinOptionsSelector");
        CreateSkinButton = GetNode<Button>("CreateSkinButton");

        CreateSkinButton.Pressed += CreateSkinButtonPressed;
    }

    public void CreateSkinButtonPressed()
    {
        SkinCreatorPopup.In();

        SkinCreator skinCreator = new()
        {
            Name = "test",
            SkinOptions = SkinOptionsSelector.SkinOptions,
        };

        CancellationTokenSource = new CancellationTokenSource();
		Skin skin = skinCreator.Create(CancellationTokenSource.Token);

        var skinInfoInstance = SkinInfoScene.Instantiate<SkinInfo>();
        skinInfoInstance.SetSkin(skin);
        EmitSignal(SignalName.ScenePushed, (StackScene)skinInfoInstance);
    }
}
