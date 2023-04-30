namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

public partial class OperationComponent : HBoxContainer
{
    public Action UndoPressed { get; set; }

    public Operation Operation { get; set; }

    private Label DescriptionLabel;
    private Label TimeStartedLabel;
    private Button UndoButton;

    public override void _Ready()
    {
        DescriptionLabel = GetNode<Label>("%DescriptionLabel");
        TimeStartedLabel = GetNode<Label>("%TimeStartedLabel");
        UndoButton = GetNode<Button>("%UndoButton");

        DescriptionLabel.Text = Operation.Description;
        TimeStartedLabel.Text = (DateTime.Now - Operation.TimeStarted.GetValueOrDefault()).Humanise();
        UndoButton.Visible = Operation.CanUndo;

        UndoButton.Pressed += OnUndoButtonPressed;
    }

    private void OnUndoButtonPressed()
    {
        UndoButton.Disabled = true;
        Operation.UndoOperation();
        UndoPressed?.Invoke();
    }
}
