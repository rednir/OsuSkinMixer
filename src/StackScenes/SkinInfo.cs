using Godot;
using System;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.StackScenes;

public partial class SkinInfo : StackScene
{
	public override string Title => "Skin info";

	private Skin Skin { get; set; }

	private Label SkinNameLabel;

	public override void _Ready()
	{
		SkinNameLabel = GetNode<Label>("%SkinNameLabel");
		SkinNameLabel.Text = Skin.Name;
	}

	public void SetSkin(Skin skin)
	{
		Skin = skin;
	}
}
