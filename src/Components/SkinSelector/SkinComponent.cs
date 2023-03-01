using System;
using Godot;
using OsuSkinMixer.Models.Osu;

namespace OsuSkinMixer.Components.SkinSelector;

public partial class SkinComponent : HBoxContainer
{
	public OsuSkin Skin { get; set; }

	public Action Pressed { get; set; }

	private Button Button;
	private Label NameLabel;
	private Label AuthorLabel;
	private Hitcircle Hitcircle;

	public override void _Ready()
	{
		Button = GetNode<Button>("%Button");
		NameLabel = GetNode<Label>("%Name");
		AuthorLabel = GetNode<Label>("%Author");
		Hitcircle = GetNode<Hitcircle>("%Hitcircle");

		Button.Pressed += OnButtonPressed;
		SetValues();
	}

	public void SetValues()
	{
		NameLabel.Text = Skin.Name;
		AuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
		Hitcircle.SetSkin(Skin);
	}

	private void OnButtonPressed()
		=> Pressed.Invoke();
}
