using Godot;
using System;

namespace OsuSkinMixer.Components;

public partial class SkinNamePopup : Control
{
	private AnimationPlayer AnimationPlayer;
	private LineEdit LineEdit;
	private Button ConfirmButton;

	private Action<string> ConfirmAction { get; set; }

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		LineEdit = GetNode<LineEdit>("%LineEdit");
		ConfirmButton = GetNode<Button>("%ConfirmButton");

		ConfirmButton.Pressed += () => ConfirmAction?.Invoke(LineEdit.Text);
		LineEdit.TextSubmitted += t => ConfirmAction?.Invoke(t);
	}

	public void In(Action<string> onConfirm)
	{
		ConfirmAction = onConfirm;
		AnimationPlayer.Play("in");
	}

	public void Out()
		=> AnimationPlayer.Play("out");
}
