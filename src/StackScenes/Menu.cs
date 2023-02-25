using Godot;
using System;
using OsuSkinMixer.StackScenes;

namespace OsuSkinMixer;

public partial class Menu : StackScene
{
	public override string Title => "Menu";

	private PackedScene SkinMixerScene;
	private PackedScene SkinModifierScene;

	private Button SkinMixerButton;
	private Button SkinModifierButton;

	public override void _Ready()
	{
		SkinMixerScene = GD.Load<PackedScene>("res://src/StackScenes/SkinMixer.tscn");
		SkinModifierScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifier.tscn");

		SkinMixerButton = GetNode<Button>("%SkinMixerButton");
		SkinModifierButton = GetNode<Button>("%SkinModifierButton");

		SkinMixerButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinMixerScene.Instantiate<StackScene>());
		SkinModifierButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinModifierScene.Instantiate<StackScene>());
	}
}
