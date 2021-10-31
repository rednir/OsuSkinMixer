using Godot;
using System;

namespace OsuSkinMixer
{
    public class Dialog : Panel
    {
        private Func<string, bool> TextInputFunc;

        private AnimationPlayer AnimationPlayer;
        private Label Label;
        private LineEdit LineEdit;

        public override void _Ready()
        {
            GetNode<Button>("Button").Connect("pressed", this, "_ButtonPressed");

            AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            Label = GetNode<Label>("LabelContainer/Label");
            LineEdit = GetNode<LineEdit>("LineEdit");
        }

        public void Alert(string text)
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Visible = false;
            TextInputFunc = null;
        }

        public void TextInput(string text, Func<string, bool> func, string defaultText = "")
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Text = defaultText;
            LineEdit.Visible = true;
            TextInputFunc = func;
        }

        private void _ButtonPressed()
        {
            AnimationPlayer.Play(!TextInputFunc?.Invoke(LineEdit.Text) ?? false ? "invalid-input" : "out");
        }
    }
}