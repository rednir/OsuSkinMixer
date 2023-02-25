using Godot;
using OsuSkinMixer.Components.SkinSelector;
using System;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifier : StackScene
{
	public override string Title => "Skin Modifier";

	private PackedScene SkinModifierModificationSelectScene;

	private VBoxContainer SkinsToModifyContainer;
	private Button AddSkinToModifyButton;
	private Button ContinueButton;
	private SkinSelectorPopup SkinSelectorPopup;

	public override void _Ready()
	{
		SkinModifierModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");

		SkinsToModifyContainer = GetNode<VBoxContainer>("%SkinsToModifyContainer");
		AddSkinToModifyButton = GetNode<Button>("%AddSkinToModifyButton");
		ContinueButton = GetNode<Button>("%ContinueButton");
		SkinSelectorPopup = GetNode<SkinSelectorPopup>("%SkinSelectorPopup");

		ContinueButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinModifierModificationSelectScene.Instantiate<StackScene>());
		AddSkinToModifyButton.Pressed += AddSkinToModifyButtonPressed;
	}

	private void AddSkinToModifyButtonPressed()
	{
		SkinSelectorPopup.In();
	}
}
