using Godot;
using System;

namespace OsuSkinMixer
{
    public class Dialog : Panel
    {
        private Action<string> TextInputAction;

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
            TextInputAction = null;
        }

        public void TextInput(string text, Action<string> action, string defaultText = "")
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Text = defaultText;
            LineEdit.Visible = true;
            TextInputAction = action;
        }

        private void _ButtonPressed()
        {
            AnimationPlayer.Play("out");
            TextInputAction?.Invoke(LineEdit.Text);
        }
    }
}