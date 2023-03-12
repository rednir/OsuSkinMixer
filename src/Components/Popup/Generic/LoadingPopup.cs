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
		CancelButton.Disabled = progress >= DisableCancelAt;

		if (progress <= 0 || progress >= 100)
		{
			if (progress >= 100)
				LoadingAnimationPlayer.Play("finish");

			LoadingAnimationPlayer.Queue("unknown");
			return;
		}
		else if (LoadingAnimationPlayer.PlaybackActive)
		{
			LoadingAnimationPlayer.Stop();
		}

		ProgressBar.Value = progress;
	}

	public override void In()
	{
		SetProgress(0);
		base.In();
	}

	private void OnCancelButtonPressed()
	{
		SetProgress(100);
		CancelAction();
	}
}
