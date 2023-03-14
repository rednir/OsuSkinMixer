using Godot;
using System.Collections.Generic;
using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;
using OsuSkinMixer.StackScenes;
using OsuSkinMixer.Models;
using System.Linq;
using System.Threading.Tasks;

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
	private Toast Toast;
	private SettingsPopup SettingsPopup;
	private LoadingPopup UpdateInProgressPopup;
	private Label VersionLabel;

	private Stack<StackScene> SceneStack { get; } = new();

	private StackScene PendingScene { get; set; }

	private Task _downloadUpdateTask;

	private int _closeRequestCount;

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
		UpdateInProgressPopup = GetNode<LoadingPopup>("%UpdateInProgressPopup");
		Toast = GetNode<Toast>("Toast");
		SettingsPopup = GetNode<SettingsPopup>("SettingsPopup");
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
				PendingScene.ToastPushed += Toast.Push;
				PendingScene.Visible = true;
				SceneStack.Push(PendingScene);
				ScenesContainer.AddChild(PendingScene);

				PendingScene = null;
				ScenesAnimationPlayer.Play("push_in");
			}

			BackButton.Disabled = SceneStack.Count <= 1;
			TitleLabel.Text = SceneStack.Peek()?.Title ?? "osu! skin mixer";
		};

		BackButton.Pressed += PopScene;
		SettingsButton.Pressed += SettingsPopup.In;

		OsuData.AllSkinsLoaded += PopAllScenes;
		OsuData.SkinAdded += s => Toast.Push($"Skin was created:\n{s.Name}");
		OsuData.SkinModified += s => Toast.Push($"Skin was modified:\n{s.Name}");
		OsuData.SkinRemoved += s => Toast.Push($"Skin was deleted:\n{s.Name}");
		OsuData.SkinInfoRequested += s =>
		{
			var instance = SkinInfoScene.Instantiate<SkinInfo>();
			instance.Skins = s;
			PushScene(instance);
		};
		OsuData.SkinModifyRequested += s =>
		{
			var scene = SkinModiferModificationSelectScene.Instantiate<SkinModifierModificationSelect>();
			scene.SkinsToModify = s.ToList();
			PushScene(scene);
		};

		PushScene(MenuScene.Instantiate<StackScene>());
		CheckForUpdates();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			if (_downloadUpdateTask == null)
				return;

			GetTree().AutoAcceptQuit = false;

			if (_closeRequestCount > 0 || _downloadUpdateTask.IsCompleted)
			{
				GetTree().Quit();
				return;
			}

			_closeRequestCount++;
			UpdateInProgressPopup.In();

			UpdateInProgressPopup.CancelAction = () => GetTree().Quit();
			_downloadUpdateTask.ContinueWith(_ => GetTree().Quit());
		}
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

	private void CheckForUpdates()
	{
		Settings.GetLatestReleaseOrNullAsync().ContinueWith(t =>
		{
			if (t.IsFaulted)
			{
				GD.PrintErr(t.Exception);
				Toast.Push("Failed to check for updates.");
			}

			GithubRelease release = t.Result;

			if (release == null)
				return;

			if (OS.GetName() != "Windows" || !Settings.Content.AutoUpdate)
			{
				// Do not auto-update.
				UpdateAvailable(release);
				return;
			}

			_downloadUpdateTask = Settings.DownloadInstallerAsync(release).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					GD.PrintErr(t.Exception);
					Toast.Push("Failed to download update.\nPlease report this error.");
				}

				UpdateAvailable(release);
			});
		});
	}

	private void UpdateAvailable(GithubRelease release)
	{
		GetNode<AnimationPlayer>("%UpdateAnimationPlayer").Play("available");
		SettingsButton.Text = $"Update to {release.TagName}";
		SettingsPopup.ShowUpdateButton();
	}
}
