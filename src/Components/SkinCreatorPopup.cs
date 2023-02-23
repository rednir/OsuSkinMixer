using Godot;

namespace OsuSkinMixer.Components;

public partial class SkinCreatorPopup : Control
{
	private AnimationPlayer AnimationPlayer;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
	}

	public void In()
	{
		AnimationPlayer.Play("in");
	}
}
