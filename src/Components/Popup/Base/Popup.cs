namespace OsuSkinMixer.Components;

public abstract partial class Popup : Control
{
    public event Action PopupIn;

    public event Action PopupOut;

    protected virtual bool IsImportant => false;

    protected AnimationPlayer AnimationPlayer { get; private set; }

    private readonly Object _lock = new();

    private bool _isCurrentlyIn;

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
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } && !IsImportant)
            Out();
    }

    public virtual void In()
    {
        _isCurrentlyIn = true;
        AnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "in");
        PopupIn?.Invoke();
    }

    public virtual void Out()
    {
        lock (_lock)
        {
            if (!_isCurrentlyIn)
                return;

            _isCurrentlyIn = false;
        }

        AnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "out");
        PopupOut?.Invoke();
    }
}
