using Godot;
using System;

namespace OsuSkinMixer;

public partial class SetupPopup : Control
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
