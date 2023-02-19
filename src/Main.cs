using Godot;
using System;
using System.Collections.Generic;
using OsuSkinMixer.StackScenes;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Components;

namespace OsuSkinMixer;

public partial class Main : Control
{
	private PackedScene MenuScene;

	private AnimationPlayer ScenesAnimationPlayer;
	private Control ScenesContainer;
	private TextureButton BackButton;
	private Label TitleLabel;
	private SetupPopup SetupPopup;

	private Stack<StackScene> SceneStack { get; } = new();

	private StackScene PendingScene { get; set; }

	public override void _Ready()
	{
		GD.Print($"osu! skin mixer {Settings.VERSION}");

		MenuScene = GD.Load<PackedScene>("res://src/StackScenes/Menu.tscn");

		ScenesAnimationPlayer = GetNode<AnimationPlayer>("ScenesAnimationPlayer");
		ScenesContainer = GetNode<Control>("Scenes/ScrollContainer");
		BackButton = GetNode<TextureButton>("TopBar/HBoxContainer/BackButton");
		TitleLabel = GetNode<Label>("TopBar/HBoxContainer/Title");
		SetupPopup = GetNode<SetupPopup>("SetupPopup");

		ScenesAnimationPlayer.AnimationFinished += (animationName) =>
		{
			if (animationName == "pop_out")
			{
				SceneStack.Pop().QueueFree();
				SceneStack.Peek().Visible = true;

				ScenesAnimationPlayer.Play("pop_in");
			}
			else if (animationName == "push_out")
			{
				if (SceneStack.TryPeek(out StackScene currentlyActiveScene))
					currentlyActiveScene.Visible = false;

				PendingScene.ScenePushed += PushScene;
				PendingScene.Visible = true;
				SceneStack.Push(PendingScene);
				ScenesContainer.AddChild(PendingScene);

				PendingScene = null;
				ScenesAnimationPlayer.Play("push_in");
			}

			TitleLabel.Text = SceneStack.Peek().Title;
		};

		BackButton.Pressed += PopScene;

		PushScene(MenuScene.Instantiate<StackScene>());

		if (!OsuData.TryLoadSkins())
			SetupPopup.In();
	}

	private void PushScene(StackScene scene)
	{
		GD.Print($"Pushing scene {scene.Title}");
		PendingScene = scene;
		ScenesAnimationPlayer.Play("push_out");
	}

	private void PopScene()
	{
		GD.Print("Popping scene");
		ScenesAnimationPlayer.Play("pop_out");
	}
}
