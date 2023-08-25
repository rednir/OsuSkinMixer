namespace OsuSkinMixer.Components;

public partial class Toast : Control
{
    const double NEW_TOAST_IF_PROGRESS_AFTER = 0.5;

    private AnimationPlayer ToastAnimationPlayer;
    private AnimationPlayer TextAnimationPlayer;
    private Label ToastTextLabel;
    private Button ToastCloseButton;

    private readonly Queue<string> _queue = new();

    public override void _Ready()
    {
        ToastAnimationPlayer = GetNode<AnimationPlayer>("%ToastAnimationPlayer");
        TextAnimationPlayer = GetNode<AnimationPlayer>("%TextAnimationPlayer");
        ToastTextLabel = GetNode<Label>("%ToastText");
        ToastCloseButton = GetNode<Button>("%ToastClose");

        ToastAnimationPlayer.AnimationFinished += (animationName) =>
        {
            if (animationName == "in")
                ToastAnimationPlayer.Play("progress");

            if (animationName == "progress")
                ToastAnimationPlayer.Play("out");
        };

        TextAnimationPlayer.AnimationFinished += (animationName) =>
        {
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
        ToastAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "in");
    }
}
