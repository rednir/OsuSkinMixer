using Godot;
using System;

namespace OsuSkinMixer.Components;

public partial class LoadingPopup : Popup
{
	protected override bool IsImportant => true;

	public Action CancelAction { get; set; }

	public double DisableCancelAt { get; set; } = 100;

	private ProgressBar ProgressBar;
	private Button CancelButton;

	public override void _Ready()
	{
		base._Ready();
		ProgressBar = GetNode<ProgressBar>("%ProgressBar");
		CancelButton = GetNode<Button>("%CancelButton");

		CancelButton.Pressed += OnCancelButtonPressed;
	}

	public void SetProgress(double progress)
	{
		ProgressBar.Value = progress;
		CancelButton.Disabled = progress >= DisableCancelAt;
	}

	private void OnCancelButtonPressed()
	{
		CancelAction();
	}
}
