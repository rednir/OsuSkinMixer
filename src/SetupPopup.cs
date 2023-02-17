using Godot;
using System;
using Environment = System.Environment;

namespace OsuSkinMixer;

public partial class SetupPopup : Control
{
	private AnimationPlayer AnimationPlayer;
	private LineEdit LineEdit;
	private Button DoneButton;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		LineEdit = GetNode<LineEdit>("Popup/ScrollContainer/VBoxContainer/ContentPanelContainer/VBoxContainer/LineEdit");
		DoneButton = GetNode<Button>("Popup/ScrollContainer/VBoxContainer/ContentPanelContainer/VBoxContainer/DoneButton");

		DoneButton.Pressed += DoneButtonPressed;
	}

	public void In()
	{
		LineEdit.Text = Settings.Content.SkinsFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu!/Skins";
		AnimationPlayer.Play("in");
	}

	private void DoneButtonPressed()
	{
		Settings.Content.SkinsFolder = LineEdit.Text;
		Settings.Save();
		AnimationPlayer.Play("out");
	}
}
