using Godot;
using OsuSkinMixer.Components.SkinOptionsSelector;
using System;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierModificationSelect : StackScene
{
	public override string Title => "Modifying skin 'skin name'";

	private SkinOptionsSelector SkinOptionsSelector;

	public override void _Ready()
	{
		SkinOptionsSelector = GetNode<SkinOptionsSelector>("SkinOptionsSelector");

		SkinOptionsSelector.CreateOptionComponents("<<UNCHANGED>>");
	}
}
