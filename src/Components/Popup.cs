using Godot;

namespace OsuSkinMixer.Components;

public partial class Popup : Control
{
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
		=> AnimationPlayer.Play("in");

    public virtual void Out()
        => AnimationPlayer.Play("out");
}
