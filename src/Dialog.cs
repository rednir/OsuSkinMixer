using Godot;
using System;

namespace OsuSkinMixer
{
    // TODO: THIS FILE SUCKS AND I NEED TO REFACTOR!!!
    // TODO: THIS FILE SUCKS AND I NEED TO REFACTOR!!!
    // TODO: THIS FILE SUCKS AND I NEED TO REFACTOR!!!
    // TODO: THIS FILE SUCKS AND I NEED TO REFACTOR!!!
    // TODO: THIS FILE SUCKS AND I NEED TO REFACTOR!!!
    public class Dialog : Panel
    {
        private Func<string, bool> TextInputFunc;
        private Action<bool> QuestionAction;
        private Action<int> OptionAction;

        private AnimationPlayer AnimationPlayer;
        private Label Label;
        private Button OkButton;
        private HBoxContainer QuestionButtons;
        private LineEdit LineEdit;
        private OptionButton OptionButton;
        private FileDialog FileDialog;

        public override void _Ready()
        {
            AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            Label = GetNode<Label>("LabelContainer/Label");
            OkButton = GetNode<Button>("OkButton");
            QuestionButtons = GetNode<HBoxContainer>("QuestionButtons");
            LineEdit = GetNode<LineEdit>("LineEdit");
            OptionButton = GetNode<OptionButton>("OptionButton");
            FileDialog = GetNode<FileDialog>("FileDialog");

            OkButton.Connect("pressed", this, nameof(_OkButtonPressed));
            FileDialog.Connect("dir_selected", this, nameof(_DirSelected));
            GetNode<Button>("QuestionButtons/YesButton").Connect("pressed", this, nameof(_YesButtonPressed));
            GetNode<Button>("QuestionButtons/NoButton").Connect("pressed", this, nameof(_NoButtonPressed));
            GetNode<Button>("LineEdit/FolderButton").Connect("pressed", this, nameof(_FolderButtonPressed));
        }

        public void Alert(string text)
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Visible = false;
            OkButton.Visible = true;
            QuestionButtons.Visible = false;
            TextInputFunc = null;
            OptionButton.Visible = false;
            OptionAction = null;
        }

        public void TextInput(string text, Func<string, bool> func, string defaultText = "")
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Visible = true;
            OkButton.Visible = true;
            QuestionButtons.Visible = false;
            TextInputFunc = func;
            OptionButton.Visible = false;
            OptionAction = null;

            LineEdit.Text = defaultText;
        }

        public void Question(string text, Action<bool> action)
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Visible = false;
            OkButton.Visible = false;
            QuestionButtons.Visible = true;
            TextInputFunc = null;
            OptionButton.Visible = false;
            OptionAction = null;

            QuestionAction = action;
        }

        public void Options(string text, Godot.Collections.Array items, Action<int> actionFromIdx)
        {
            Label.Text = text;
            AnimationPlayer.Play("in");

            LineEdit.Visible = false;
            OkButton.Visible = true;
            QuestionButtons.Visible = false;
            TextInputFunc = null;
            OptionButton.Visible = true;
            OptionAction = actionFromIdx;

            OptionButton.Items = items;
        }

        private void _OkButtonPressed()
        {
            OptionAction?.Invoke(OptionButton.Selected);
            OptionAction = null;

            AnimationPlayer.Play(!TextInputFunc?.Invoke(LineEdit.Text) ?? false ? "invalid-input" : "out");
            TextInputFunc = null;
        }

        private void _FolderButtonPressed()
        {
            FileDialog.CurrentDir = LineEdit.Text;
            FileDialog.Popup_();
        }

        private void _DirSelected(string path)
        {
            LineEdit.Text = path;
        }

        private void _YesButtonPressed()
        {
            QuestionAction?.Invoke(true);
            QuestionAction = null;
            AnimationPlayer.Play("out");
        }

        private void _NoButtonPressed()
        {
            QuestionAction?.Invoke(false);
            QuestionAction = null;
            AnimationPlayer.Play("out");
        }
    }
}