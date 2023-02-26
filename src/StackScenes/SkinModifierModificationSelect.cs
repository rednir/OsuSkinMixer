using Godot;
using System;
using System.Collections.Generic;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;
using OsuSkinMixer.Components;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierModificationSelect : StackScene
{
	public override string Title => SkinsToModify.Count == 1 ? $"Modifying: {SkinsToModify[0].Name}" : $"Modifying {SkinsToModify.Count} skins";

	public List<Skin> SkinsToModify { get; set; }

	private SkinOptionsSelector SkinOptionsSelector;
	private Button ApplyChangesButton;
	private SkinCreatorPopup SkinCreatorPopup;

	public override void _Ready()
	{
		SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
		ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
		SkinCreatorPopup = GetNode<SkinCreatorPopup>("%SkinCreatorPopup");

		SkinOptionsSelector.CreateOptionComponents("<<UNCHANGED>>");
		ApplyChangesButton.Pressed += OnApplyChangesButtonPressed;
	}

	private void OnApplyChangesButtonPressed()
	{
		SkinCreatorPopup.In();
	}
}
