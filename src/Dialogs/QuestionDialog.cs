using Godot;
using System;
using Array = Godot.Collections.Array;

namespace OsuSkinMixer
{
    public class QuestionDialog : DialogBase
    {
        private static readonly PackedScene Scene = GD.Load<PackedScene>($"res://src/Dialogs/{nameof(QuestionDialog)}.tscn");

        public static void New(Node target, string text, Action<bool> action)
        {
            var dialog = Scene.Instance<QuestionDialog>();
            dialog.LabelText = text;
            dialog.Action = action;
            target.AddChild(dialog);
        }

        protected Action<bool> Action { get; set; }

        public override void _Ready()
        {
            GetNode<Button>("Dialog/HBoxContainer/YesButton").Connect("pressed", this, nameof(_OnButtonPressed), new Array(true));
            GetNode<Button>("Dialog/HBoxContainer/NoButton").Connect("pressed", this, nameof(_OnButtonPressed), new Array(false));
            base._Ready();
        }

        public void _OnButtonPressed(bool value)
        {
            Action.Invoke(value);
            Out();
        }
    }
}
