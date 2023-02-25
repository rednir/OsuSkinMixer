using Godot;

namespace OsuSkinMixer.Components;

public partial class SkinCreatorPopup : Popup
{
	private ProgressBar ProgressBar;

	public override void _Ready()
	{
		base._Ready();
		ProgressBar = GetNode<ProgressBar>("%ProgressBar");
	}

	public void SetProgress(float progress)
	{
		ProgressBar.Value = progress;
	}
}
