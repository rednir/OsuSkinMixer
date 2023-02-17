using Godot;
using System;
using System.Collections.Generic;

namespace OsuSkinMixer;

public partial class Main : Control
{
	private PackedScene MenuScene;

	private Control ScenesControl;

	private readonly Stack<StackScene> SceneStack = new();

	public override void _Ready()
	{
		MenuScene = GD.Load<PackedScene>("res://src/Menu.tscn");

		ScenesControl = GetNode<Control>("Scenes");

		PushScene(MenuScene.Instantiate<StackScene>());
	}

	public override void _Process(double delta)
	{
	}

	private void PushScene(StackScene scene)
	{
		scene.ScenePushed += PushScene;
		SceneStack.Push(scene);
		ScenesControl.AddChild(scene);
	}
}
