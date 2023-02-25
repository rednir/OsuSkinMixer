using Godot;
using System;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifier : StackScene
{
	public override string Title => "Skin Modifier";

	private PackedScene SkinModifierModificationSelectScene;

	private Button ContinueButton;

	public override void _Ready()
	{
		SkinModifierModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");

		ContinueButton = GetNode<Button>("%ContinueButton");

		ContinueButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinModifierModificationSelectScene.Instantiate<StackScene>());
	}
}
