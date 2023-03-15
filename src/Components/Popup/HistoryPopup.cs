using Godot;
using OsuSkinMixer.Statics;
using System.Linq;

namespace OsuSkinMixer.Components;

public partial class HistoryPopup : Popup
{
    private PackedScene OperationComponentScene = GD.Load<PackedScene>("res://src/Components/OperationComponent.tscn");

    private VBoxContainer OperationComponentContainer;

    public override void _Ready()
    {
        base._Ready();

        OperationComponentContainer = GetNode<VBoxContainer>("%OperationComponentContainer");
    }

    public override void In()
    {
        InitaliseOperationComponents();
        base.In();
    }

    private void InitaliseOperationComponents()
    {
        foreach (var child in OperationComponentContainer.GetChildren())
            child.QueueFree();

        foreach (var operation in Enumerable.Reverse(Settings.Content.Operations))
        {
            var operationComponent = OperationComponentScene.Instantiate<OperationComponent>();
            operationComponent.Operation = operation;
            operationComponent.UndoPressed += InitaliseOperationComponents;
            OperationComponentContainer.AddChild(operationComponent);
        }
    }
}
