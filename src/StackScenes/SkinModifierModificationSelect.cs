using Godot;
using System;
using System.Collections.Generic;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierModificationSelect : StackScene
{
	public override string Title => "Modifying skin 'skin name'";

	public List<Skin> SkinsToModify { get; set; }

	private SkinOptionsSelector SkinOptionsSelector;

	public override void _Ready()
	{
		SkinOptionsSelector = GetNode<SkinOptionsSelector>("SkinOptionsSelector");

		SkinOptionsSelector.CreateOptionComponents("<<UNCHANGED>>");
	}
}
