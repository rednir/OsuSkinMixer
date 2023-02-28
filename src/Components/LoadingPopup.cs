using Godot;
using System;

namespace OsuSkinMixer.Components;

public partial class LoadingPopup : Popup
{
	protected override bool IsImportant => true;

	public Action CancelAction { get; set; }

	private ProgressBar ProgressBar;
	private Button CancelButton;

	public override void _Ready()
	{
		base._Ready();
		ProgressBar = GetNode<ProgressBar>("%ProgressBar");
		CancelButton = GetNode<Button>("%CancelButton");

		CancelButton.Pressed += OnCancelButtonPressed;
	}

	public void SetProgress(float progress)
	{
		ProgressBar.Value = progress;
	}

	private void OnCancelButtonPressed()
	{
		CancelButton.Disabled = true;
		CancelAction();
		CancelButton.Disabled = false;
	}
}
