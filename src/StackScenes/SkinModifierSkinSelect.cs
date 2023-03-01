using System.Collections.Generic;
using Godot;
using OsuSkinMixer.Components.SkinSelector;
using OsuSkinMixer.Models.Osu;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierSkinSelect : StackScene
{
	public override string Title => "Skin Modifier";

	private List<OsuSkin> SkinsToModify { get; set; } = new();

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

	private void OnSkinSelected(OsuSkin skin)
	{
		if (SkinsToModify.Contains(skin))
		{
			EmitSignal(SignalName.ToastPushed, "You already selected this skin!");
			return;
		}

		var component = SkinComponentScene.Instantiate<SkinComponent>();
		component.Skin = skin;
		component.Pressed += () =>
		{
			SkinsToModify.Remove(skin);
			component.QueueFree();

			if (SkinsToModify.Count == 0)
				ContinueButton.Disabled = true;
		};
		SkinsToModifyContainer.AddChild(component);

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
