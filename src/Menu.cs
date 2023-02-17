using Godot;
using System;

namespace OsuSkinMixer;

public partial class Menu : StackScene
{
	public override string Title => "Menu";

	private PackedScene SkinMixerScene;

	private Button TempButton;

	public override void _Ready()
	{
		SkinMixerScene = GD.Load<PackedScene>("res://src/SkinMixer.tscn");

		TempButton = GetNode<Button>("Button");
		TempButton.Connect("pressed", new Callable(this, nameof(TempButtonPressed)));
	}

	private void TempButtonPressed()
	{
		EmitSignal(SignalName.ScenePushed, SkinMixerScene.Instantiate<StackScene>());
	}
}
