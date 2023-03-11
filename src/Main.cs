using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;
using OsuSkinMixer.StackScenes;
using OsuSkinMixer.Models;

namespace OsuSkinMixer;

public partial class Main : Control
{
	private PackedScene MenuScene;
	private PackedScene SkinModiferModificationSelectScene;
	private PackedScene SkinInfoScene;

	private CanvasLayer Background;
	private AnimationPlayer ScenesAnimationPlayer;
	private Control ScenesContainer;
	private Button BackButton;
	private Label TitleLabel;
	private Button SettingsButton;
	private SettingsPopup SettingsPopup;
	private AnimationPlayer ToastAnimationPlayer;
	private Label ToastTextLabel;
	private Button ToastCloseButton;
	private Label VersionLabel;

	private Stack<StackScene> SceneStack { get; } = new();

	private StackScene PendingScene { get; set; }

	public override void _Ready()
	{
		MenuScene = GD.Load<PackedScene>("res://src/StackScenes/Menu.tscn");
		SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");
		SkinModiferModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");

		Background = GetNode<CanvasLayer>("Background");
		ScenesAnimationPlayer = GetNode<AnimationPlayer>("ScenesAnimationPlayer");
		ScenesContainer = GetNode<Control>("Scenes/ScrollContainer");
		BackButton = GetNode<Button>("TopBar/HBoxContainer/BackButton");
		TitleLabel = GetNode<Label>("TopBar/HBoxContainer/Title");
		SettingsButton = GetNode<Button>("TopBar/HBoxContainer/SettingsButton");
		SettingsPopup = GetNode<SettingsPopup>("SettingsPopup");
		ToastAnimationPlayer = GetNode<AnimationPlayer>("%ToastAnimationPlayer");
		ToastTextLabel = GetNode<Label>("%ToastText");
		ToastCloseButton = GetNode<Button>("%ToastClose");
		VersionLabel = GetNode<Label>("%VersionLabel");

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
			TitleLabel.Text = SceneStack.Peek()?.Title ?? "osu! skin mixer";
		};

		ToastAnimationPlayer.AnimationFinished += (animationName) =>
		{
			if (animationName == "in")
				ToastAnimationPlayer.Play("out");
		};

		ToastCloseButton.Pressed += () => ToastAnimationPlayer.Play("out");
		BackButton.Pressed += PopScene;
		SettingsButton.Pressed += SettingsPopup.In;

		OsuData.AllSkinsLoaded += PopAllScenes;
		OsuData.SkinAdded += s => PushToast($"Skin was created:\n{s.Name}");
		OsuData.SkinModified += s => PushToast($"Skin was modified:\n{s.Name}");
		OsuData.SkinRemoved += s => PushToast($"Skin was deleted:\n{s.Name}");
		OsuData.SkinInfoRequested += s =>
		{
			var instance = SkinInfoScene.Instantiate<SkinInfo>();
			instance.Skins = new OsuSkin[] { s };
			PushScene(instance);
		};
		OsuData.SkinModifyRequested += s =>
		{
			var scene = SkinModiferModificationSelectScene.Instantiate<SkinModifierModificationSelect>();
			scene.SkinsToModify = new List<OsuSkin> { s };
			PushScene(scene);
		};

		PushScene(MenuScene.Instantiate<StackScene>());
		CheckForUpdates();
	}

	public override void _Process(double delta)
	{
		float value = GetViewportRect().Size.Y / 450;
		Background.Scale = new Vector2(value, value);
		Background.Offset = new Vector2(GetViewportRect().Size.X / 2, 0);
	}

	private void PushScene(StackScene scene)
	{
		Settings.Log($"Pushing scene {scene.Title}");
		PendingScene = scene;
		ScenesAnimationPlayer.Play("push_out");
	}

	private void PopScene()
	{
		Settings.Log("Popping scene");
		ScenesAnimationPlayer.Play("pop_out");
	}

	private void PopAllScenes()
	{
		if (SceneStack.Count <= 1)
			return;

		Settings.Log("Popping all scenes and returning to menu");
		while (SceneStack.Count > 2)
			SceneStack.Pop().QueueFree();

		SceneStack.Peek().Visible = true;
		ScenesAnimationPlayer.Play("pop_out");
	}

	private void PushToast(string text)
	{
		ToastTextLabel.Text = text;
		ToastAnimationPlayer.Stop();
		ToastAnimationPlayer.Play("in");
	}

	private void CheckForUpdates()
	{
		var req = GetNode<HttpRequest>("HTTPRequest");
		req.RequestCompleted += OnHttpRequestCompleted;
		req.Request($"https://api.github.com/repos/{Settings.GITHUB_REPO_PATH}/releases/latest", new string[] { "User-Agent: OsuSkinMixer" });
	}

	private void OnHttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0)
			return;

		try
		{
			string latest = JsonSerializer.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(body))["tag_name"].ToString();
			if (latest != Settings.VERSION)
			{
				GetNode<AnimationPlayer>("%UpdateAnimationPlayer").Play("available");
				SettingsButton.Text = $"Update to {latest}";
				SettingsPopup.ShowUpdateButton();
			}
		}
		catch
		{
			PushToast("Failed to check for updates");
		}
	}
}
