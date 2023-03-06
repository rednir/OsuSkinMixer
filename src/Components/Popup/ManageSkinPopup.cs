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
	private Label TitleLabel;
	private Button ModifyButton;
	private Button HideButton;
	private Button DeleteButton;

	private OsuSkin _skin;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		DeleteQuestionPopup = GetNode<QuestionPopup>("%DeleteQuestionPopup");
		TitleLabel = GetNode<Label>("%Title");
		ModifyButton = GetNode<Button>("%ModifyButton");
		HideButton = GetNode<Button>("%HideButton");
		DeleteButton = GetNode<Button>("%DeleteButton");

		DeleteQuestionPopup.ConfirmAction = OnDeleteConfirmed;
		ModifyButton.Pressed += OnModifyButtonPressed;
		HideButton.Pressed += OnHideButtonPressed;
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
}
