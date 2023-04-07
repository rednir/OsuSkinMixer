using Godot;
using System;

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
            CancelButton.Disabled = value >= DisableCancelAt;

            if (value <= 0 || value >= 100)
            {
                if (value >= 100 && !LoadingAnimationPlayer.IsPlaying())
                    LoadingAnimationPlayer.Play("finish");

                LoadingAnimationPlayer.Queue("unknown");
                return;
            }
            else if (LoadingAnimationPlayer.PlaybackActive)
            {
                LoadingAnimationPlayer.Stop();
            }

            ProgressBar.Value = value;
        }
    }

    public string ProgressStatusText
    {
        get => ProgressStatus.Text;
        set
        {
            ProgressStatus.Text = value;
            StatusAnimationPlayer.Play("new");
        }
    }

    private AnimationPlayer LoadingAnimationPlayer;
    private AnimationPlayer StatusAnimationPlayer;
    private ProgressBar ProgressBar;
    private Label ProgressStatus;
    private Button CancelButton;

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

    public override void In()
    {
        Progress = 0;
        base.In();
    }

    private void OnCancelButtonPressed()
    {
        Progress = 0;
        CancelAction();
    }
}
