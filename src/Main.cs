using Godot;
using System;
using System.Collections.Generic;

namespace OsuSkinMixer;

public partial class Main : Control
{
	private PackedScene MenuScene;

	private Control LoadedScenesControl;

	private readonly Stack<StackScene> LoadedScenes = new();

	public override void _Ready()
	{
		MenuScene = GD.Load<PackedScene>("res://src/Menu.tscn");

		LoadedScenesControl = GetNode<Control>("LoadedScenes");

		PushScene(MenuScene.Instantiate<StackScene>());
	}

	public override void _Process(double delta)
	{
	}

	private void PushScene(StackScene scene)
	{
		scene.ScenePushed += PushScene;
		LoadedScenes.Push(scene);
		LoadedScenesControl.AddChild(scene);
	}
}
