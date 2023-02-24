using Godot;
using System;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.Components.SkinSelector;

public partial class SkinComponent : HBoxContainer
{
	public Button Button { get; private set; }

	private Label NameLabel;
	private Hitcircle Hitcircle;

	public override void _Ready()
	{
		Button = GetNode<Button>("Button");
		NameLabel = GetNode<Label>("Button/Name");
		Hitcircle = GetNode<Hitcircle>("Button/Hitcircle");
	}

	public void SetValues(Skin skin)
	{
		NameLabel.Text = skin.Name;
		Hitcircle.SetSkin(skin);
	}
}
