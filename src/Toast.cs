using Godot;
using System;

namespace OsuSkinMixer
{
    public class Toast : CenterContainer
    {
        private Label Label;
        private AnimationPlayer AnimationPlayer;

        public override void _Ready()
        {
            Label = GetNode<Label>("Label");
            AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        }

        public void New(string text)
        {
            AnimationPlayer.Stop();
            Label.Text = text;
            AnimationPlayer.Play("new");
        }
    }
}