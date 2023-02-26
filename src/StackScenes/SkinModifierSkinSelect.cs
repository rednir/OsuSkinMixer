using Godot;
using System;
using System.Collections.Generic;
using OsuSkinMixer.Components.SkinSelector;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierSkinSelect : StackScene
{
	public override string Title => "Skin Modifier";

	private List<Skin> SkinsToModify { get; set; } = new();

	private PackedScene SkinModifierModificationSelectScene;
	private PackedScene SkinComponentScene;

	private VBoxContainer SkinsToModifyContainer;
	private Button AddSkinToModifyButton;
	private Button ContinueButton;
	private SkinSelectorPopup SkinSelectorPopup;

	public override void _Ready()
	{
		SkinModifierModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");
		SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinSelector/SkinComponent.tscn");

		SkinsToModifyContainer = GetNode<VBoxContainer>("%SkinsToModifyContainer");
		AddSkinToModifyButton = GetNode<Button>("%AddSkinToModifyButton");
		ContinueButton = GetNode<Button>("%ContinueButton");
		SkinSelectorPopup = GetNode<SkinSelectorPopup>("%SkinSelectorPopup");

		ContinueButton.Pressed += OnContinueButtonPressed;
		AddSkinToModifyButton.Pressed += AddSkinToModifyButtonPressed;

		SkinSelectorPopup.OnSelected = OnSkinSelected;
	}

	private void AddSkinToModifyButtonPressed()
	{
		SkinSelectorPopup.In();
	}

	private void OnSkinSelected(Skin skin)
	{
		if (SkinsToModify.Contains(skin))
		{
			OS.Alert("Skin already selected to modify", "Skin modifier");
			return;
		}

		var component = SkinComponentScene.Instantiate<SkinComponent>();
		SkinsToModifyContainer.AddChild(component);
		component.SetValues(skin);

		component.Button.Pressed += () =>
		{
			SkinsToModify.Remove(skin);
			component.QueueFree();

			if (SkinsToModify.Count == 0)
				ContinueButton.Disabled = true;
		};

		SkinsToModify.Add(skin);
		ContinueButton.Disabled = false;
	}

	private void OnContinueButtonPressed()
	{
		var instance = SkinModifierModificationSelectScene.Instantiate<SkinModifierModificationSelect>();
		instance.SkinsToModify = SkinsToModify;
		EmitSignal(SignalName.ScenePushed, instance);
	}
}
