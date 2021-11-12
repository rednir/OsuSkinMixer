using Godot;
using System;

namespace OsuSkinMixer
{
    public class OkDialog : DialogBase
    {
        private static readonly PackedScene Scene = GD.Load<PackedScene>($"res://src/Dialogs/{nameof(OkDialog)}.tscn");

        public static void New(Node target, string text)
        {
            var dialog = Scene.Instance<OkDialog>();
            dialog.LabelText = text;
            target.AddChild(dialog);
        }

        public override void _Ready()
        {
            GetNode<Button>("Dialog/Button").Connect("pressed", this, nameof(_OnButtonPressed));
            base._Ready();
        }

        public void _OnButtonPressed() => Out();
    }
}