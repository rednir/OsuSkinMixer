using System;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Utils;

namespace OsuSkinMixer.Components;

public partial class SkinInfoPanel : PanelContainer
{
	public OsuSkin Skin { get; set; }

	private VBoxContainer DeletedContainer;
	private VBoxContainer MainContentContainer;
	private QuestionPopup DeleteQuestionPopup;
	private SkinPreview SkinPreview;
	private HitcircleIcon HitcircleIcon;
	private Label SkinNameLabel;
	private Label SkinAuthorLabel;
	private Button DeleteButton;
	private Button HideButton;
	private Label DetailsLabel;
	private Button OpenFolderButton;
	private Button OpenInOsuButton;

	public override void _Ready()
	{
		DeletedContainer = GetNode<VBoxContainer>("%DeletedContainer");
		MainContentContainer = GetNode<VBoxContainer>("%MainContentContainer");
		DeleteQuestionPopup = GetNode<QuestionPopup>("%DeleteQuestionPopup");
		SkinPreview = GetNode<SkinPreview>("%SkinPreview");
		HitcircleIcon = GetNode<HitcircleIcon>("%HitcircleIcon");
		SkinNameLabel = GetNode<Label>("%SkinName");
		SkinAuthorLabel = GetNode<Label>("%SkinAuthor");
		DeleteButton = GetNode<Button>("%DeleteButton");
		HideButton = GetNode<Button>("%HideButton");
		DetailsLabel = GetNode<Label>("%Details");
		OpenFolderButton = GetNode<Button>("%OpenFolderButton");
		OpenInOsuButton = GetNode<Button>("%OpenInOsuButton");

		DeleteQuestionPopup.ConfirmAction = OnDeleteConfirmed;
		SkinPreview.SetSkin(Skin);
		HitcircleIcon.SetSkin(Skin);
		SkinNameLabel.Text = Skin.Name;
		SkinAuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
		DetailsLabel.Text = $"Last modified: {Skin.Directory.LastWriteTime}";
		DeleteButton.Pressed += OnDeleteButtonPressed;
		OpenFolderButton.Pressed += OnOpenFolderButtonPressed;
		OpenInOsuButton.Pressed += OnOpenInOsuButtonPressed;
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
        SkinMachine.TriggerOskImport(Skin);
	}
}
