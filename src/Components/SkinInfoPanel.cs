using Godot;
using System;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Components;

namespace OsuSkinMixer.StackScenes;

public partial class SkinInfoPanel : PanelContainer
{
	public OsuSkin Skin { get; set; }

	private Sprite2D Cursor;
	private CpuParticles2D Cursortrail;
	private Hitcircle Hitcircle;
	private Label SkinNameLabel;
	private Label SkinAuthorLabel;
	private Button MoreButton;
	private TextureRect MenuBackground;
	private Label DetailsLabel;
	private Button OpenFolderButton;
	private Button OpenInOsuButton;

	public override void _Ready()
	{
		Cursor = GetNode<Sprite2D>("%Cursor");
		Cursortrail = GetNode<CpuParticles2D>("%Cursortrail");
		Hitcircle = GetNode<Hitcircle>("%Hitcircle");
		SkinNameLabel = GetNode<Label>("%SkinName");
		SkinAuthorLabel = GetNode<Label>("%SkinAuthor");
		MoreButton = GetNode<Button>("%MoreButton");
		MenuBackground = GetNode<TextureRect>("%MenuBackground");
		DetailsLabel = GetNode<Label>("%Details");
		OpenFolderButton = GetNode<Button>("%OpenFolderButton");
		OpenInOsuButton = GetNode<Button>("%OpenInOsuButton");

		Cursor.Texture = Skin.Cursor;
		Cursortrail.Texture = Skin.Cursortrail;
		Hitcircle.SetSkin(Skin);
		SkinNameLabel.Text = Skin.Name;
		SkinAuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
		MenuBackground.Texture = Skin.MenuBackground;
		DetailsLabel.Text = $"Last modified: {Skin.Directory.LastWriteTime}";
		OpenFolderButton.Pressed += OpenFolderButtonPressed;
		OpenInOsuButton.Pressed += OpenInOsuButtonPressed;
	}

	public override void _Process(double delta)
	{
		Cursor.GlobalPosition = GetGlobalMousePosition();
	}

	private void OpenFolderButtonPressed()
	{
		OS.ShellOpen(Skin.Directory.FullName);
	}

	private void OpenInOsuButtonPressed()
	{
		SkinCreator.TriggerOskImport(Skin);
	}
}
