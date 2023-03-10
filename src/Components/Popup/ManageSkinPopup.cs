using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OsuSkinMixer.Components;

public partial class ManageSkinPopup : Popup
{
	private const string HIDE_BUTTON_TEXT = "Hide from osu!";

	private const string UNHIDE_BUTTON_TEXT = "Unhide from osu!";

	private QuestionPopup DeleteQuestionPopup;
	private SkinNamePopup SkinNamePopup;
	private LoadingPopup LoadingPopup;
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
		LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");
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
		OsuData.RequestSkinModify(_skin);
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
		Settings.Log($"Duplicating skin: {_skin.Name} -> {newSkinName}");
		LoadingPopup.In();

		Task.Run(() =>
		{
			OsuSkin newSkin = new(_skin.Directory.CopyDirectory(Path.Combine(Settings.SkinsFolderPath, newSkinName), true));
			OsuData.AddSkin(newSkin);
			OsuData.RequestSkinInfo(newSkin);
		})
		.ContinueWith(t =>
		{
			LoadingPopup.Out();
			SkinNamePopup.Out();

			if (t.IsFaulted)
			{
				GD.PrintErr(t.Exception);
				OS.Alert("Failed to duplicate skin. Please report this issue with logs.", "Error");
			}
		});
	}

	private void OnDeleteButtonPressed()
	{
		DeleteQuestionPopup.In();
	}

	private void OnDeleteConfirmed()
	{
		Settings.Log($"Deleting skin: {_skin.Name}");
		LoadingPopup.In();

		Task.Run(() =>
		{
			_skin.Directory.Delete(true);
			OsuData.RemoveSkin(_skin);
		})
		.ContinueWith(t =>
		{
			LoadingPopup.Out();
			Out();

			if (t.IsFaulted)
			{
				GD.PrintErr(t.Exception);
				OS.Alert("Failed to delete skin. Please report this issue with logs.", "Error");
			}
		});
	}
}
