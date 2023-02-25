using Godot;
using OsuSkinMixer.Statics;
using System;
using System.IO;
using System.Linq;

namespace OsuSkinMixer.Components;

public partial class Popup : Control
{
    protected AnimationPlayer AnimationPlayer { get; private set; }

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
	}

    public virtual void In()
		=> AnimationPlayer.Play("in");

    public virtual void Out()
        => AnimationPlayer.Play("out");
}
