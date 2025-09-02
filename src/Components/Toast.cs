namespace OsuSkinMixer.Components;

public partial class Toast : Control
{
    const double NEW_TOAST_IF_PROGRESS_AFTER = 0.5;

    private PanelContainer Panel;
    private AnimationPlayer ToastAnimationPlayer;
    private AnimationPlayer TextAnimationPlayer;
    private Label ToastTextLabel;
    private Button ToastCloseButton;

    private readonly Queue<string> _queue = new();

    public override void _Ready()
    {
        Panel = GetNode<PanelContainer>("%Panel");
        ToastAnimationPlayer = GetNode<AnimationPlayer>("%ToastAnimationPlayer");
        TextAnimationPlayer = GetNode<AnimationPlayer>("%TextAnimationPlayer");
        ToastTextLabel = GetNode<Label>("%ToastText");
        ToastCloseButton = GetNode<Button>("%ToastClose");

        ToastAnimationPlayer.AnimationStarted += (animationName) =>
        {
            // Because jank.
            Panel.Size = new Vector2(Panel.Size.X, 30);
        };

        ToastAnimationPlayer.AnimationFinished += (animationName) =>
        {
            if (animationName == "in")
            {
                ToastAnimationPlayer.Play("progress");
            }

            ToastAnimationPlayer.SpeedScale = 1.0f;

            if (animationName == "progress")
                ToastAnimationPlayer.Play("out");
        };

        TextAnimationPlayer.AnimationFinished += (animationName) =>
        {
            ToastAnimationPlayer.SpeedScale = ToastTextLabel.Text.Length > 80 ? 0.5f : 1.0f;

            if (animationName == "in")
                NextText();
        };

        ToastCloseButton.Pressed += () => ToastAnimationPlayer.Play("out");
    }

    public void Push(string text)
    {
        if (ToastAnimationPlayer.AssignedAnimation is "in" or "progress"
            && (ToastAnimationPlayer.CurrentAnimationPosition / ToastAnimationPlayer.CurrentAnimationLength) < NEW_TOAST_IF_PROGRESS_AFTER)
        {
            _queue.Enqueue(text);
            NextText();
            return;
        }

        _queue.Clear();
        ToastTextLabel.SetDeferred(Label.PropertyName.Text, text);
        ToastAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "in");
    }

    private void NextText()
    {
        if (_queue.Count == 0 || TextAnimationPlayer.CurrentAnimation == "in")
            return;

        ToastTextLabel.SetDeferred(Label.PropertyName.Text, _queue.Dequeue());
        TextAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "in");

        ToastAnimationPlayer.SpeedScale = 1.0f;
    }
}
