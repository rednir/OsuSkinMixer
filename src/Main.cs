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
	private PackedScene SettingsScene;

	private CanvasLayer Background;
	private AnimationPlayer ScenesAnimationPlayer;
	private Control ScenesContainer;
	private TextureButton BackButton;
	private Label TitleLabel;
	private Button SettingsButton;
	private SetupPopup SetupPopup;
	private AnimationPlayer ToastAnimationPlayer;
	private Label ToastTextLabel;
	private TextureButton ToastCloseButton;
	private Label VersionLabel;

	private Stack<StackScene> SceneStack { get; } = new();

	private StackScene PendingScene { get; set; }

	public override void _Ready()
	{
		GD.Print($"osu! skin mixer {Settings.VERSION} at {DateTime.Now}");

		DisplayServer.WindowSetTitle("osu! skin mixer by rednir");
		DisplayServer.WindowSetMinSize(new Vector2I(600, 300));

		MenuScene = GD.Load<PackedScene>("res://src/StackScenes/Menu.tscn");
		SettingsScene = GD.Load<PackedScene>("res://src/StackScenes/SettingsScene.tscn");

		Background = GetNode<CanvasLayer>("Background");
		ScenesAnimationPlayer = GetNode<AnimationPlayer>("ScenesAnimationPlayer");
		ScenesContainer = GetNode<Control>("Scenes/ScrollContainer");
		BackButton = GetNode<TextureButton>("TopBar/HBoxContainer/BackButton");
		TitleLabel = GetNode<Label>("TopBar/HBoxContainer/Title");
		SettingsButton = GetNode<Button>("TopBar/HBoxContainer/SettingsButton");
		SetupPopup = GetNode<SetupPopup>("SetupPopup");
		ToastAnimationPlayer = GetNode<AnimationPlayer>("%ToastAnimationPlayer");
		ToastTextLabel = GetNode<Label>("%ToastText");
		ToastCloseButton = GetNode<TextureButton>("%ToastClose");
		VersionLabel = GetNode<Label>("VersionLabel");

		VersionLabel.Text = Settings.VERSION;

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
				PendingScene.ToastPushed += PushToast;
				PendingScene.Visible = true;
				SceneStack.Push(PendingScene);
				ScenesContainer.AddChild(PendingScene);

				PendingScene = null;
				ScenesAnimationPlayer.Play("push_in");
			}

			BackButton.Disabled = SceneStack.Count <= 1;
			SettingsButton.Disabled = SceneStack.Peek() is SettingsScene;
			TitleLabel.Text = SceneStack.Peek().Title;
		};

		ToastAnimationPlayer.AnimationFinished += (animationName) =>
		{
			if (animationName == "in")
				ToastAnimationPlayer.Play("out");
		};

		ToastCloseButton.Pressed += () => ToastAnimationPlayer.Play("out");

		BackButton.Pressed += PopScene;
		SettingsButton.Pressed += () => PushScene(SettingsScene.Instantiate<StackScene>());

		PushScene(MenuScene.Instantiate<StackScene>());

		if (!OsuData.TryLoadSkins())
			SetupPopup.In();
	}

	public override void _Process(double delta)
	{
        float value = GetViewportRect().Size.Y / 450;
		Background.Scale = new Vector2(value, value);
		Background.Offset = new Vector2(GetViewportRect().Size.X / 2, 0);
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

	private void PushToast(string text)
	{
		ToastTextLabel.Text = text;
		ToastAnimationPlayer.Stop();
		ToastAnimationPlayer.Play("in");
	}
}
