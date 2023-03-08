using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;
using System.IO;

namespace OsuSkinMixer.Components;

public partial class ManageSkinPopup : Popup
{
	private const string HIDE_BUTTON_TEXT = "Hide from osu!";

	private const string UNHIDE_BUTTON_TEXT = "Unhide from osu!";

	public Action ModifySkin { get; set; }

	private QuestionPopup DeleteQuestionPopup;
	private SkinNamePopup SkinNamePopup;
	private Label TitleLabel;
	private Button ModifyButton;
	private Button HideButton;
	private Button DuplicateButton;
	private Button DeleteButton;

	private OsuSkin _skin;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		DeleteQuestionPopup = GetNode<QuestionPopup>("%DeleteQuestionPopup");
		SkinNamePopup = GetNode<SkinNamePopup>("%SkinNamePopup");
		TitleLabel = GetNode<Label>("%Title");
		ModifyButton = GetNode<Button>("%ModifyButton");
		HideButton = GetNode<Button>("%HideButton");
		DuplicateButton = GetNode<Button>("%DuplicateButton");
		DeleteButton = GetNode<Button>("%DeleteButton");

		DeleteQuestionPopup.ConfirmAction = OnDeleteConfirmed;
		SkinNamePopup.ConfirmAction = OnDuplicateSkinNameConfirmed;
		SkinNamePopup.PopupOut += Out;
		ModifyButton.Pressed += OnModifyButtonPressed;
		HideButton.Pressed += OnHideButtonPressed;
		DuplicateButton.Pressed += OnDuplicateButtonPressed;
		DeleteButton.Pressed += OnDeleteButtonPressed;
	}

	public void SetSkin(OsuSkin skin)
	{
		_skin = skin;
		TitleLabel.Text = skin.Name;
		HideButton.Text = skin.Hidden ? UNHIDE_BUTTON_TEXT : HIDE_BUTTON_TEXT;
	}

	private void OnModifyButtonPressed()
	{
		ModifySkin();
		Out();
	}

	private void OnHideButtonPressed()
	{
		OsuData.ToggleSkinHiddenState(_skin);
		Out();
	}

	private void OnDuplicateButtonPressed()
	{
		SkinNamePopup.LineEditText = $"{_skin.Name} (copy)";
		SkinNamePopup.In();
	}

	private void OnDuplicateSkinNameConfirmed(string newSkinName)
	{
		try
		{
			Settings.Log($"Duplicating skin: {_skin.Name} -> {newSkinName}");
			OsuSkin newSkin = new(CopyDirectory(_skin.Directory, Path.Combine(Settings.SkinsFolderPath, newSkinName)));
			OsuData.AddSkin(newSkin);
			OsuData.RequestSkinInfo(newSkin);
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			OS.Alert("Failed to duplicate skin. Please report this issue with logs.", "Error");
		}

		SkinNamePopup.Out();
	}

	private void OnDeleteButtonPressed()
	{
		DeleteQuestionPopup.In();
	}

	private void OnDeleteConfirmed()
	{
		try
		{
			Settings.Log($"Deleting skin: {_skin.Name}");
			_skin.Directory.Delete(true);
			OsuData.RemoveSkin(_skin);
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			OS.Alert("Failed to delete skin. Please report this issue with logs.", "Error");
		}

		Out();
	}

    private static DirectoryInfo CopyDirectory(DirectoryInfo sourceDir, string destinationDir)
    {
        DirectoryInfo[] dirs = sourceDir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in sourceDir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir, newDestinationDir);
        }

        return new DirectoryInfo(destinationDir);
    }
}
