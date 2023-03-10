using System;
using Godot;

namespace OsuSkinMixer.Components;

public abstract partial class Popup : Control
{
	public event Action PopupIn;

	public event Action PopupOut;

	protected virtual bool IsImportant => false;

    protected AnimationPlayer AnimationPlayer { get; private set; }

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");

		GetNode<ScrollContainer>("Popup/CanvasLayer/ScrollContainer").GuiInput += OnGuiInputOutsideContent;
		GetNode<VBoxContainer>("Popup/CanvasLayer/ScrollContainer/VBoxContainer").GuiInput += OnGuiInputOutsideContent;
	}

    public override void _Input(InputEvent inputEvent)
    {
		if (inputEvent.IsActionPressed("ui_cancel") && !IsImportant)
			Out();
    }

	private void OnGuiInputOutsideContent(InputEvent inputEvent)
	{
		if ((inputEvent as InputEventMouseButton)?.ButtonIndex == MouseButton.Left && !IsImportant)
			Out();
	}

    public virtual void In()
    {
        AnimationPlayer.Play("in");
		PopupIn?.Invoke();
    }

    public virtual void Out()
    {
        AnimationPlayer.Play("out");
		PopupOut?.Invoke();
    }
}
