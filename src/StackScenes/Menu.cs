using System;
using Godot;
using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.StackScenes;

public partial class Menu : StackScene
{
    public override string Title => "Menu";

    private PackedScene SkinMixerScene;
    private PackedScene SkinModifierSkinSelectScene;
    private PackedScene SkinManagerScene;

    private Button SkinMixerButton;
    private Button SkinModifierButton;
    private Button SkinManagerButton;
    private Button GetMoreSkinsButton;
    private Button LuckyButton;
    private GetMoreSkinsPopup GetMoreSkinsPopup;

    private readonly Random _random = new();

    public override void _Ready()
    {
        SkinMixerScene = GD.Load<PackedScene>("res://src/StackScenes/SkinMixer.tscn");
        SkinModifierSkinSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierSkinSelect.tscn");
        SkinManagerScene = GD.Load<PackedScene>("res://src/StackScenes/SkinManager.tscn");

        SkinMixerButton = GetNode<Button>("%SkinMixerButton");
        SkinModifierButton = GetNode<Button>("%SkinModifierButton");
        SkinManagerButton = GetNode<Button>("%SkinManagerButton");
        GetMoreSkinsButton = GetNode<Button>("%GetMoreSkinsButton");
        GetMoreSkinsPopup = GetNode<GetMoreSkinsPopup>("%GetMoreSkinsPopup");
        LuckyButton = GetNode<Button>("%LuckyButton");

        SkinMixerButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinMixerScene.Instantiate<StackScene>());
        SkinModifierButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinModifierSkinSelectScene.Instantiate<StackScene>());
        SkinManagerButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinManagerScene.Instantiate<StackScene>());
        GetMoreSkinsButton.Pressed += GetMoreSkinsPopup.In;
        LuckyButton.Pressed += OnLuckyButtonPressed;
    }

    private void OnLuckyButtonPressed()
    {
        int randomIndex = _random.Next(0, OsuData.Skins.Length);
        OsuData.RequestSkinInfo(new[] { OsuData.Skins[randomIndex] });
    }
}
