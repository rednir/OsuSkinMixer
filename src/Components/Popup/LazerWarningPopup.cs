namespace OsuSkinMixer.Components;

public partial class LazerWarningPopup : Popup
{
    protected override bool IsImportant => true;

    private CheckBox UnderstandCheckBox;
    private Button CancelButton;
    private Button ProceedButton;
    private VBoxContainer NormalTextVBox;
    private VBoxContainer MismatchTextVBox;
    private TaskCompletionSource<bool> PendingChoice;

    public override void _Ready()
    {
        base._Ready();
        UnderstandCheckBox = GetNode<CheckBox>("%UnderstandCheckBox");
        CancelButton = GetNode<Button>("%CancelButton");
        ProceedButton = GetNode<Button>("%ProceedButton");
        NormalTextVBox = GetNode<VBoxContainer>("%NormalTextVBox");
        MismatchTextVBox = GetNode<VBoxContainer>("%MismatchTextVBox");

        UnderstandCheckBox.Toggled += accepted => ProceedButton.Disabled = !accepted;
        CancelButton.Pressed += () => Complete(false);
        ProceedButton.Pressed += () => Complete(true);
    }

    public Task<bool> ConfirmAsync(bool schemaMismatch = false, bool includeNormalWarning = false)
    {
        var previousChoice = PendingChoice;
        PendingChoice = null;
        previousChoice?.TrySetResult(false);
        PendingChoice = new TaskCompletionSource<bool>();
        NormalTextVBox.Visible = !schemaMismatch || includeNormalWarning;
        MismatchTextVBox.Visible = schemaMismatch;
        UnderstandCheckBox.ButtonPressed = false;
        ProceedButton.Disabled = true;
        In();
        return PendingChoice.Task;
    }

    private void Complete(bool accepted)
    {
        if (accepted && !UnderstandCheckBox.ButtonPressed)
            return;

        Out();
        var choice = PendingChoice;
        PendingChoice = null;
        choice?.TrySetResult(accepted);
    }

    public override void _ExitTree()
    {
        PendingChoice?.TrySetResult(false);
        PendingChoice = null;
    }
}
