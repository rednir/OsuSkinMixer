using Godot;
using System;

namespace OsuSkinMixer
{
    public abstract class DialogBase : Control
    {
        private AnimationPlayer AnimationPlayer;

        protected string LabelText { get; set; }

        public override void _Ready()
        {
            AnimationPlayer = GetNode<AnimationPlayer>("Dialog/AnimationPlayer");
            AnimationPlayer.Connect("animation_finished", this, nameof(_AnimationFinished));
            GetNode<Label>("Dialog/Label").Text = LabelText;
            In();
        }

        protected void In() => AnimationPlayer.Play("in");

        protected void Out() => AnimationPlayer.Play("out");

        private void _AnimationFinished(string name)
        {
            if (name == "out")
                QueueFree();
        }
    }
}