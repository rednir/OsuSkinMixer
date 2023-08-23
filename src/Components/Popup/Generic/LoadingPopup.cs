namespace OsuSkinMixer.Components;

public partial class LoadingPopup : Popup
{
    protected override bool IsImportant => true;

    public Action CancelAction { get; set; }

    public double DisableCancelAt { get; set; } = 100;

    public double Progress
    {
        get => ProgressBar.Value;
        set
        {
            CancelButton.SetDeferred(Button.PropertyName.Disabled, value >= DisableCancelAt);

            if (value <= 0 || value >= 100)
            {
                if (value >= 100 && !LoadingAnimationPlayer.IsPlaying())
                    LoadingAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "finish");

                LoadingAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Queue, "unknown");
                return;
            }
            else if (LoadingAnimationPlayer.PlaybackActive)
            {
                LoadingAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Stop);
            }

            ProgressBar.SetDeferred(ProgressBar.PropertyName.Value, value);
        }
    }

    public string Status
    {
        get => ProgressStatus.Text;
        set => _pendingStatus = value;
    }

    private AnimationPlayer LoadingAnimationPlayer;
    private AnimationPlayer StatusAnimationPlayer;
    private ProgressBar ProgressBar;
    private Label ProgressStatus;
    private Button CancelButton;

    private string _pendingStatus;

    public override void _Ready()
    {
        base._Ready();
        LoadingAnimationPlayer = GetNode<AnimationPlayer>("%LoadingAnimationPlayer");
        StatusAnimationPlayer = GetNode<AnimationPlayer>("%StatusAnimationPlayer");
        ProgressBar = GetNode<ProgressBar>("%ProgressBar");
        ProgressStatus = GetNode<Label>("%ProgressStatus");
        CancelButton = GetNode<Button>("%CancelButton");

        CancelButton.Pressed += OnCancelButtonPressed;
    }

    public override void _Process(double delta)
    {
        if (_pendingStatus != null)
        {
            StatusAnimationPlayer.Stop();
            StatusAnimationPlayer.Play("new");
            ProgressStatus.Text = _pendingStatus;
            _pendingStatus = null;
        }
    }

    public override void In()
    {
        Progress = 0;
        ProgressStatus.Text = "";
        base.In();
    }

    private void OnCancelButtonPressed()
    {
        Progress = 0;
        CancelAction();
    }
}
