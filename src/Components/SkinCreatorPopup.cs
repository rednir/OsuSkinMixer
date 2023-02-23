using Godot;

namespace OsuSkinMixer.Components;

public partial class SkinCreatorPopup : Control
{
	private AnimationPlayer AnimationPlayer;
	private ProgressBar ProgressBar;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		ProgressBar = GetNode<ProgressBar>("%ProgressBar");
	}

	public void In()
		=> AnimationPlayer.Play("in");

	public void Out()
		=> AnimationPlayer.Play("out");

	public void SetProgress(float progress)
	{
		ProgressBar.Value = progress;
	}
}
