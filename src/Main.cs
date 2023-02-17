using Godot;
using System;
using System.Collections.Generic;

namespace OsuSkinMixer;

public partial class Main : Control
{
	private PackedScene MenuScene;

	private AnimationPlayer ScenesAnimationPlayer;
	private Control ScenesControl;

	private Stack<StackScene> SceneStack { get; } = new();

	private StackScene PendingScene { get; set; }

	public override void _Ready()
	{
		MenuScene = GD.Load<PackedScene>("res://src/Menu.tscn");

		ScenesAnimationPlayer = GetNode<AnimationPlayer>("ScenesAnimationPlayer");
		ScenesControl = GetNode<Control>("Scenes");

		ScenesAnimationPlayer.AnimationFinished += (animationName) =>
		{
			if (animationName != "scene_out" || PendingScene == null)
				return;

			if (SceneStack.TryPeek(out StackScene currentlyActiveScene))
				currentlyActiveScene.Visible = false;

			PendingScene.ScenePushed += PushScene;
			PendingScene.Visible = true;
			SceneStack.Push(PendingScene);
			ScenesControl.AddChild(PendingScene);

			PendingScene = null;
			ScenesAnimationPlayer.Play("scene_in");
		};

		PushScene(MenuScene.Instantiate<StackScene>());
	}

	private void PushScene(StackScene scene)
	{
		PendingScene = scene;
		ScenesAnimationPlayer.Play("scene_out");
	}
}
