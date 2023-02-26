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
	private Button FolderPickerButton;
	private FileDialog FileDialog;

	public override void _Ready()
	{
		base._Ready();

		LineEdit = GetNode<LineEdit>("%LineEdit");
		DoneButton = GetNode<Button>("%DoneButton");
		FolderPickerButton = GetNode<Button>("%FolderPickerButton");
		FileDialog = GetNode<FileDialog>("%FileDialog");

		DoneButton.Pressed += DoneButtonPressed;
		FolderPickerButton.Pressed += FolderPickerButtonPressed;
		FileDialog.DirSelected += d => LineEdit.Text = d;
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

	private void FolderPickerButtonPressed()
	{
		FileDialog.CurrentDir = LineEdit.Text;
		FileDialog.PopupCentered();
	}
}
