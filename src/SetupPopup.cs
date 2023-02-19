using Godot;
using System;
using Environment = System.Environment;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer;

public partial class SetupPopup : Control
{
	private AnimationPlayer AnimationPlayer;
	private LineEdit LineEdit;
	private Button DoneButton;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		LineEdit = GetNode<LineEdit>("Popup/CanvasLayer/ScrollContainer/VBoxContainer/ContentPanelContainer/VBoxContainer/LineEdit");
		DoneButton = GetNode<Button>("Popup/CanvasLayer/ScrollContainer/VBoxContainer/ContentPanelContainer/VBoxContainer/DoneButton");

		DoneButton.Pressed += DoneButtonPressed;
	}

	public void In()
	{
		LineEdit.Text = Settings.Content.SkinsFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu!/Skins";
		AnimationPlayer.Play("in");
	}

	private void DoneButtonPressed()
	{
		if (Settings.TrySetSkinsFolder(LineEdit.Text))
		{
			Settings.Save();
			AnimationPlayer.Play("out");
			return;
		}

		OS.Alert("The path you entered is invalid. Please try again.", "Error");
	}
}
