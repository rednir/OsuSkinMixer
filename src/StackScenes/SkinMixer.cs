using Godot;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Components;
using System.Collections.Generic;

namespace OsuSkinMixer.StackScenes;

public partial class SkinMixer : StackScene
{
    public override string Title => "Skin Mixer";

    private IEnumerable<SkinOption> SkinOptions { get; } = SkinOption.Default;

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

        SkinOptionsSelector.CreateOptionComponents(SkinOptions);
    }

    public void CreateSkinButtonPressed()
    {
        SkinCreatorPopup.CreateSkin("test", SkinOptions);

        var skinInfoInstance = SkinInfoScene.Instantiate<StackScene>();
        EmitSignal(SignalName.ScenePushed, skinInfoInstance);
    }
}
