using Godot;
using System;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

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
        TimeStartedLabel.Text = Operation.TimeStarted.ToString();
        UndoButton.Disabled = !Operation.CanUndo;

        UndoButton.Pressed += OnUndoButtonPressed;
    }

    private void OnUndoButtonPressed()
    {
        UndoButton.Disabled = false;
        Operation.UndoOperation();
        UndoPressed?.Invoke();
    }
}
