using System;
using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class SkinComponent : HBoxContainer
{
	public OsuSkin Skin { get; set; }

	public Action Pressed { get; set; }

	private Button Button;
	private Label NameLabel;
	private Label AuthorLabel;
	private HitcircleIcon Hitcircle;
	private TextureRect HiddenIcon;

	public override void _Ready()
	{
		Button = GetNode<Button>("%Button");
		NameLabel = GetNode<Label>("%Name");
		AuthorLabel = GetNode<Label>("%Author");
		Hitcircle = GetNode<HitcircleIcon>("%Hitcircle");
		HiddenIcon = GetNode<TextureRect>("%HiddenIcon");

		Button.Pressed += OnButtonPressed;

		if (Skin != null)
			SetValues();
	}

	public void SetValues()
	{
		NameLabel.Text = Skin.Name;
		AuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
		Button.TooltipText = Skin.Name;
		HiddenIcon.Visible = Skin.Hidden;
		Hitcircle.SetSkin(Skin);
	}

	private void OnButtonPressed()
		=> Pressed.Invoke();
}
