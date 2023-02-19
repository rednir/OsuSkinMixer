using Godot;
using System;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.Components;

public partial class SkinComponent : HBoxContainer
{
	public Button Button { get; private set; }

	private Label NameLabel;

	public override void _Ready()
	{
		Button = GetNode<Button>("Button");
		NameLabel = GetNode<Label>("Button/Name");
	}

	public void SetValues(Skin skin)
	{
		NameLabel.Text = skin.Name;
	}
}
