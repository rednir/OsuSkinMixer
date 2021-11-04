using Godot;
using System;

namespace OsuSkinMixer
{
    // TODO: next time im forced to look at this i need to rewrite this entire thing.
    public class Dialog : Panel
    {
        private Func<string, bool> TextInputFunc;
        private Action<bool> QuestionAction;

        private AnimationPlayer AnimationPlayer;
        private Label Label;
        private Button OkButton;
        private HBoxContainer QuestionButtons;
        private LineEdit LineEdit;

        public override void _Ready()
        {
            AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            Label = GetNode<Label>("LabelContainer/Label");
            OkButton = GetNode<Button>("OkButton");
            QuestionButtons = GetNode<HBoxContainer>("QuestionButtons");
            LineEdit = GetNode<LineEdit>("LineEdit");

            OkButton.Connect("pressed", this, "_OkButtonPressed");
            GetNode<Button>("QuestionButtons/YesButton").Connect("pressed", this, "_YesButtonPressed");
            GetNode<Button>("QuestionButtons/NoButton").Connect("pressed", this, "_NoButtonPressed");
        }

        public void Alert(string text)
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Visible = false;
            OkButton.Visible = true;
            QuestionButtons.Visible = false;

            TextInputFunc = null;
        }

        public void TextInput(string text, Func<string, bool> func, string defaultText = "")
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Visible = true;
            OkButton.Visible = true;
            QuestionButtons.Visible = false;

            LineEdit.Text = defaultText;
            TextInputFunc = func;
        }

        public void Question(string text, Action<bool> action)
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Visible = false;
            OkButton.Visible = false;
            QuestionButtons.Visible = true;

            TextInputFunc = null;

            QuestionAction = action;
        }

        private void _OkButtonPressed()
        {
            AnimationPlayer.Play(!TextInputFunc?.Invoke(LineEdit.Text) ?? false ? "invalid-input" : "out");
        }

        private void _YesButtonPressed()
        {
            QuestionAction?.Invoke(true);
            AnimationPlayer.Play("out");
        }

        private void _NoButtonPressed()
        {
            QuestionAction?.Invoke(false);
            AnimationPlayer.Play("out");
        }
    }
}