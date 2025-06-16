global using Godot;
global using System;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Collections.Generic;
global using System.Collections.Concurrent;

namespace OsuSkinMixer;

using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;
using OsuSkinMixer.StackScenes;
using OsuSkinMixer.Models;
using System.IO;

public partial class Main : Control
{
    private PackedScene MenuScene;
    private PackedScene SkinModiferModificationSelectScene;
    private PackedScene SkinInfoScene;

    private AnimationPlayer ScenesAnimationPlayer;
    private AnimationPlayer UpdateAnimationPlayer;
    private AnimationPlayer HomeButtonAnimationPlayer;
    private Background Background;
    private Control ScenesContainer;
    private Button BackButton;
    private Button HomeButton;
    private Label TitleLabel;
    private Button SettingsButton;
    private SettingsPopup SettingsPopup;
    private Button HistoryButton;
    private HistoryPopup HistoryPopup;
    private LoadingPopup ExitBlockedPopup;
    private OkPopup OkPopup;
    private Toast Toast;
    private Label VersionLabel;

    private Stack<StackScene> SceneStack { get; } = new();

    private StackScene PendingScene { get; set; }

    private List<Task> ExitBlockingTasks { get; } = new();

    private IEnumerable<OsuSkin> SkinInfoRequestedSkins { get; set; }

    private IEnumerable<OsuSkin> SkinModifyRequestedSkins { get; set; }

    private int _closeRequestCount;

    public override void _Ready()
    {
        MenuScene = GD.Load<PackedScene>("res://src/StackScenes/Menu.tscn");
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");
        SkinModiferModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");
        
        UpdateAnimationPlayer = GetNode<AnimationPlayer>("%UpdateAnimationPlayer");
        ScenesAnimationPlayer = GetNode<AnimationPlayer>("ScenesAnimationPlayer");
        HomeButtonAnimationPlayer = GetNode<AnimationPlayer>("%HomeButtonAnimationPlayer");
        Background = GetNode<Background>("Background");
        ScenesContainer = GetNode<Control>("Scenes/ScrollContainer");
        BackButton = GetNode<Button>("%BackButton");
        HomeButton = GetNode<Button>("%HomeButton");
        TitleLabel = GetNode<Label>("TopBar/HBoxContainer/Title");
        HistoryButton = GetNode<Button>("%HistoryButton");
        HistoryPopup = GetNode<HistoryPopup>("%HistoryPopup");
        SettingsButton = GetNode<Button>("%SettingsButton");
        SettingsPopup = GetNode<SettingsPopup>("SettingsPopup");
        ExitBlockedPopup = GetNode<LoadingPopup>("%ExitBlockedPopup");
        OkPopup = GetNode<OkPopup>("%OkPopup");
        Toast = GetNode<Toast>("%Toast");
        VersionLabel = GetNode<Label>("%VersionLabel");

        VersionLabel.Text = Settings.VERSION;

        ScenesAnimationPlayer.AnimationFinished += OnScenesAnimationPlayerFinished;

        BackButton.Pressed += PopScene;
        HomeButton.Pressed += PopAllScenes;
        SettingsButton.Pressed += SettingsPopup.In;
        HistoryButton.Pressed += HistoryPopup.In;

        Settings.ExceptionPushed += e =>
        {
            OkPopup.SetValues($"{e.Message}\n\nPlease report this error and attach logs.", "Something went wrong...");
            OkPopup.In();
        };

        OsuData.AllSkinsLoaded += PopAllScenes;
        OsuData.SkinAdded += s => Toast.Push($"Skin was created:\n{s.Name}");
        OsuData.SkinModified += s => Toast.Push($"Skin was modified:\n{s.Name}");
        OsuData.SkinRemoved += s => Toast.Push($"Skin was deleted:\n{s.Name}");
        OsuData.SkinInfoRequested += s => SkinInfoRequestedSkins = s;
        OsuData.SkinModifyRequested += s => SkinModifyRequestedSkins = s;

        PendingScene = MenuScene.Instantiate<StackScene>();
        OnScenesAnimationPlayerFinished("push_out");

        Settings.Content.LaunchCount++;
        
        CheckForUpdates();
    }

    public override void _Notification(int what)
    {
		if (what == NotificationApplicationFocusIn)
		{
			GetTree().Paused = false;
            Engine.MaxFps = 0;
		}
		else if (what == NotificationApplicationFocusOut)
		{
			GetTree().Paused = true;
            Engine.MaxFps = 20;
		}
        else if (what == NotificationWMCloseRequest)
        {
            GetTree().AutoAcceptQuit = false;
            Settings.Save();

            ClearTrash();

            if (_closeRequestCount > 0 || ExitBlockingTasks.Count == 0)
            {
                GetTree().Quit();
                return;
            }

            _closeRequestCount++;
            ExitBlockedPopup.CancelAction = () => GetTree().Quit();
            ExitBlockedPopup.In();

            Task.Run(async () =>
            {
                foreach (var task in ExitBlockingTasks)
                    await task;

                GetTree().Quit();
            });
        }
    }

    public override void _Process(double delta)
    {
        float value = GetViewportRect().Size.Y / 450;
        Background.Scale = new Vector2(value, value);
        Background.Offset = new Vector2(GetViewportRect().Size.X / 2, 0);

        // These events are handled on the main thread to avoid crashes.
        if (SkinInfoRequestedSkins != null)
        {
            var instance = SkinInfoScene.Instantiate<SkinInfo>();
            instance.Skins = SkinInfoRequestedSkins.ToList();
            PushScene(instance);

            SkinInfoRequestedSkins = null;
        }
        if (SkinModifyRequestedSkins != null)
        {
            var scene = SkinModiferModificationSelectScene.Instantiate<SkinModifierModificationSelect>();
            scene.SkinsToModify = SkinModifyRequestedSkins.ToList();
            PushScene(scene);

            SkinModifyRequestedSkins = null;
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("debug"))
        {   
            if (Settings.IsLoggingToFile)
            {
                Settings.StopLoggingToFile();
                Toast.Push("Logs were saved. Opening folder...");
            }
            else
            {
                Settings.StartLoggingToFile();
                Toast.Push("Debug logging enabled.\nPress Ctrl+Shift+L again to save logs.");
            }
        }
    }

    private void PushScene(StackScene scene)
    {
        if (PendingScene != null)
            return;

        Settings.Log($"Pushing scene {scene.Title}");
        PendingScene = scene;
        Background.HideSnow();
        ScenesAnimationPlayer.Play("push_out");
    }

    private void PopScene()
    {
        Settings.Log("Popping scene");
        ScenesAnimationPlayer.Play("pop_out");

        if (HomeButton.Visible && SceneStack.Count <= 3)
            HomeButtonAnimationPlayer.Play("hide");
    }

    private void PopAllScenes()
    {
        if (SceneStack.Count <= 1)
            return;

        Settings.Log("Popping all scenes and returning to menu");
        while (SceneStack.Count > 2)
            SceneStack.Pop().QueueFree();

        SceneStack.Peek().SetDeferred(PropertyName.Visible, true);
        ScenesAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "pop_out");
    }

    private void OnScenesAnimationPlayerFinished(StringName animationName)
    {
        if (animationName == "pop_out")
        {
            SceneStack.Pop().QueueFree();
            SceneStack.Peek().Visible = true;

            if (SceneStack.Count == 1)
                Background.ShowSnow();

            ScenesAnimationPlayer.Play("pop_in");
        }
        else if (animationName == "push_out")
        {
            if (SceneStack.TryPeek(out StackScene currentlyActiveScene))
                currentlyActiveScene.Visible = false;

            PendingScene.ScenePushed += PushScene;
            PendingScene.ScenePopped += PopScene;
            PendingScene.ToastPushed += Toast.Push;
            PendingScene.Visible = true;
            SceneStack.Push(PendingScene);
            ScenesContainer.AddChild(PendingScene);

            PendingScene = null;
            ScenesAnimationPlayer.Play("push_in");

            if (!HomeButton.Visible && SceneStack.Count >= 3)
                HomeButtonAnimationPlayer.Play("show");
        }

        BackButton.Disabled = SceneStack.Count <= 1;
        TitleLabel.Text = SceneStack.Peek()?.Title ?? "osu! skin mixer";
    }

    private void ClearTrash()
    {
        Task task = Task.Run(() =>
        {
            // TODO: Could just move the trash folder to the delete on exit folder.
            if (Directory.Exists(Settings.TrashFolderPath))
                Directory.Delete(Settings.TrashFolderPath, true);

            if (Directory.Exists(Settings.DeleteOnExitFolderPath))
                Directory.Delete(Settings.DeleteOnExitFolderPath, true);
        })
        .ContinueWith(t =>
        {
            if (t.IsFaulted)
                GD.PrintErr(t.Exception);
        });

        ExitBlockingTasks.Add(task);
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

            Toast.Push($"An update is being downloaded.");
            Task task = Settings.DownloadInstallerAsync(release).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    GD.PrintErr(t.Exception);
                    Toast.Push("Failed to download update.\nPlease report this error.");
                }

                UpdateAvailable(release);
            });

            ExitBlockingTasks.Add(task);
        });
    }

    private void UpdateAvailable(GithubRelease release)
    {
        UpdateAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "available");
        SettingsButton.SetDeferred(Button.PropertyName.Text, $"Update to {release.TagName}");
        SettingsPopup.ShowUpdateButton();
    }
}
