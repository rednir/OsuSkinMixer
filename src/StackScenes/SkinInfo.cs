using Godot;
using System;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.StackScenes;

public partial class SkinInfo : StackScene
{
	public override string Title => "Skin info";

	private Label SkinNameLabel;

	public override void _Ready()
	{
		SkinNameLabel = GetNode<Label>("%SkinNameLabel");
	}

	public void SetSkin(Skin skin)
	{
		SkinNameLabel.Text = skin.Name;
	}
}
