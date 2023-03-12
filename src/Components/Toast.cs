using Godot;
using System;

namespace OsuSkinMixer.Components;

public partial class Toast : PanelContainer
{
	private AnimationPlayer ToastAnimationPlayer;
	private Label ToastTextLabel;
	private Button ToastCloseButton;

    public override void _Ready()
    {
        ToastAnimationPlayer = GetNode<AnimationPlayer>("%ToastAnimationPlayer");
        ToastTextLabel = GetNode<Label>("%ToastText");
        ToastCloseButton = GetNode<Button>("%ToastClose");

		ToastAnimationPlayer.AnimationFinished += (animationName) =>
		{
			if (animationName == "in")
				ToastAnimationPlayer.Play("out");
		};

		ToastCloseButton.Pressed += () => ToastAnimationPlayer.Play("out");
    }

	public void Push(string text)
	{
		ToastTextLabel.Text = text;
		ToastAnimationPlayer.Stop();
		ToastAnimationPlayer.Play("in");
	}
}
