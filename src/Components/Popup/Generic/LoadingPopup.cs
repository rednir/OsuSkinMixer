using Godot;
using System;

namespace OsuSkinMixer.Components;

public partial class LoadingPopup : Popup
{
	protected override bool IsImportant => true;

	public Action CancelAction { get; set; }

	public double DisableCancelAt { get; set; } = 100;

	private AnimationPlayer LoadingAnimationPlayer;
	private ProgressBar ProgressBar;
	private Button CancelButton;

	public override void _Ready()
	{
		base._Ready();
		LoadingAnimationPlayer = GetNode<AnimationPlayer>("%LoadingAnimationPlayer");
		ProgressBar = GetNode<ProgressBar>("%ProgressBar");
		CancelButton = GetNode<Button>("%CancelButton");

		CancelButton.Pressed += OnCancelButtonPressed;
	}

	public void SetProgress(double progress)
	{
		if (progress <= 0)
		{
			LoadingAnimationPlayer.Play("unknown");
			return;
		}
		else if (LoadingAnimationPlayer.PlaybackActive)
		{
			LoadingAnimationPlayer.Stop();
		}

		ProgressBar.Value = progress;
		CancelButton.Disabled = progress >= DisableCancelAt;
	}

	private void OnCancelButtonPressed()
	{
		CancelAction();
	}
}
