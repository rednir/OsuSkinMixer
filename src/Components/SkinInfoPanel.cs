using System;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SkinInfoPanel : PanelContainer
{
	public OsuSkin Skin { get; set; }

	private VBoxContainer DeletedContainer;
	private VBoxContainer MainContentContainer;
	private QuestionPopup DeleteQuestionPopup;
	private Sprite2D Cursor;
	private CpuParticles2D Cursortrail;
	private Hitcircle Hitcircle;
	private Label SkinNameLabel;
	private Label SkinAuthorLabel;
	private Button DeleteButton;
	private Button HideButton;
	private TextureRect MenuBackground;
	private Label DetailsLabel;
	private Button OpenFolderButton;
	private Button OpenInOsuButton;

	public override void _Ready()
	{
		DeletedContainer = GetNode<VBoxContainer>("%DeletedContainer");
		MainContentContainer = GetNode<VBoxContainer>("%MainContentContainer");
		DeleteQuestionPopup = GetNode<QuestionPopup>("%DeleteQuestionPopup");
		Cursor = GetNode<Sprite2D>("%Cursor");
		Cursortrail = GetNode<CpuParticles2D>("%Cursortrail");
		Hitcircle = GetNode<Hitcircle>("%Hitcircle");
		SkinNameLabel = GetNode<Label>("%SkinName");
		SkinAuthorLabel = GetNode<Label>("%SkinAuthor");
		DeleteButton = GetNode<Button>("%DeleteButton");
		HideButton = GetNode<Button>("%HideButton");
		MenuBackground = GetNode<TextureRect>("%MenuBackground");
		DetailsLabel = GetNode<Label>("%Details");
		OpenFolderButton = GetNode<Button>("%OpenFolderButton");
		OpenInOsuButton = GetNode<Button>("%OpenInOsuButton");

		DeleteQuestionPopup.ConfirmAction = OnDeleteConfirmed;
		Cursor.Texture = Skin.GetTexture("cursor.png");
		Cursortrail.Texture = Skin.GetTexture("cursortrail.png");
		Hitcircle.SetSkin(Skin);
		SkinNameLabel.Text = Skin.Name;
		SkinAuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
		MenuBackground.Texture = Skin.GetTexture("menu-background.jpg") ?? Skin.GetTexture("menu-background.png");
		DetailsLabel.Text = $"Last modified: {Skin.Directory.LastWriteTime}";
		DeleteButton.Pressed += OnDeleteButtonPressed;
		OpenFolderButton.Pressed += OnOpenFolderButtonPressed;
		OpenInOsuButton.Pressed += OnOpenInOsuButtonPressed;
	}

	public override void _Process(double delta)
	{
		Cursor.GlobalPosition = GetGlobalMousePosition();
	}

	private void OnDeleteButtonPressed()
	{
		DeleteQuestionPopup.In();
	}

	private void OnDeleteConfirmed()
	{
		try
		{
			Skin.Directory.Delete(true);
			OsuData.RemoveSkin(Skin);
			MainContentContainer.Visible = false;
			DeletedContainer.Visible = true;
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
		}
	}

	private void OnOpenFolderButtonPressed()
	{
		OS.ShellOpen(Skin.Directory.FullName);
	}

	private void OnOpenInOsuButtonPressed()
	{
		SkinCreator.TriggerOskImport(Skin);
	}
}
