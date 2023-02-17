using Godot;
using System;

namespace OsuSkinMixer;

public partial class Menu : StackScene
{
	public override string Title => "Menu";

	private PackedScene SkinMixerScene;

	private Button SkinMixerButton;

	public override void _Ready()
	{
		SkinMixerScene = GD.Load<PackedScene>("res://src/SkinMixer.tscn");

		SkinMixerButton = GetNode<Button>("SkinMixerButton");
		SkinMixerButton.Connect("pressed", new Callable(this, nameof(SkinMixerButtonPressed)));
	}

	private void SkinMixerButtonPressed()
	{
		EmitSignal(SignalName.ScenePushed, SkinMixerScene.Instantiate<StackScene>());
	}
}
