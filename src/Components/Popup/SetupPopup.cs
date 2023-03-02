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
	private OkPopup OkPopup;

	public override void _Ready()
	{
		base._Ready();

		LineEdit = GetNode<LineEdit>("%LineEdit");
		DoneButton = GetNode<Button>("%DoneButton");
		FolderPickerButton = GetNode<Button>("%FolderPickerButton");
		FileDialog = GetNode<FileDialog>("%FileDialog");
		OkPopup = GetNode<OkPopup>("%OkPopup");

		DoneButton.Pressed += DoneButtonPressed;
		FolderPickerButton.Pressed += FolderPickerButtonPressed;
		FileDialog.DirSelected += d => LineEdit.Text = d;
	}

	public override void In()
	{
		base.In();
		LineEdit.Text = Settings.Content.OsuFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu!";
	}

	private void DoneButtonPressed()
    {
        if (!Settings.TrySetOsuFolder(LineEdit.Text, out string error))
        {
			OkPopup.SetValues(error, "That doesn't seem right...");
            OkPopup.In();
			return;
        }

		Settings.Save();
		AnimationPlayer.Play("out");
    }

    private void FolderPickerButtonPressed()
	{
		FileDialog.CurrentDir = LineEdit.Text;
		FileDialog.PopupCentered();
	}
}