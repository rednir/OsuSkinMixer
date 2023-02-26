using Godot;
using System;
using Environment = System.Environment;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SetupPopup : Popup
{
	protected override bool IsImportant => true;

	private LineEdit LineEdit;
	private Button DoneButton;

	public override void _Ready()
	{
		base._Ready();

		LineEdit = GetNode<LineEdit>("%LineEdit");
		DoneButton = GetNode<Button>("%DoneButton");

		DoneButton.Pressed += DoneButtonPressed;
	}

	public override void In()
	{
		base.In();
		LineEdit.Text = Settings.Content.SkinsFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu!/Skins";
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
