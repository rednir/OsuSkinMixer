using Godot;
using System.Collections.Generic;
using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;
using OsuSkinMixer.StackScenes;
using OsuSkinMixer.Models;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

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
    private Button HistoryButton;
    private HistoryPopup HistoryPopup;
    private LoadingPopup ExitBlockedPopup;
    private OkPopup OkPopup;
    private Toast Toast;
    private Label VersionLabel;

    private Stack<StackScene> SceneStack { get; } = new();

    private StackScene PendingScene { get; set; }

    private List<Task> ExitBlockingTasks { get; } = new();

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
        HistoryButton = GetNode<Button>("%HistoryButton");
        HistoryPopup = GetNode<HistoryPopup>("%HistoryPopup");
        SettingsButton = GetNode<Button>("%SettingsButton");
        SettingsPopup = GetNode<SettingsPopup>("SettingsPopup");
        ExitBlockedPopup = GetNode<LoadingPopup>("%ExitBlockedPopup");
        OkPopup = GetNode<OkPopup>("%OkPopup");
        Toast = GetNode<Toast>("Toast");
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
                PendingScene.ScenePopped += PopScene;
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

    private void ClearTrash()
    {
        if (!Directory.Exists(Settings.TrashFolderPath))
            return;

        Task task = Task.Run(() => Directory.Delete(Settings.TrashFolderPath, true))
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
        GetNode<AnimationPlayer>("%UpdateAnimationPlayer").Play("available");
        SettingsButton.Text = $"Update to {release.TagName}";
        SettingsPopup.ShowUpdateButton();
    }
}
