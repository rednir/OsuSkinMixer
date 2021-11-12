using Godot;
using System;
using Array = Godot.Collections.Array;

namespace OsuSkinMixer
{
    public class OptionsDialog : DialogBase
    {
        private static readonly PackedScene Scene = GD.Load<PackedScene>($"res://src/Dialogs/{nameof(OptionsDialog)}.tscn");

        public static void New(Node target, string text, Array items, Action<int> action)
        {
            var dialog = Scene.Instance<OptionsDialog>();
            dialog.LabelText = text;
            dialog.Action = action;
            dialog.Items = items;
            target.AddChild(dialog);
        }

        protected Action<int> Action { get; set; }

        protected Array Items { get; set; }

        private OptionButton OptionButton;

        public override void _Ready()
        {
            OptionButton = GetNode<OptionButton>("Dialog/OptionButton");
            OptionButton.Items = Items;

            GetNode<Button>("Dialog/Button").Connect("pressed", this, nameof(_OnButtonPressed));
            base._Ready();
        }

        public void _OnButtonPressed()
        {
            Action.Invoke(OptionButton.Selected);
            Out();
        }
    }
}
