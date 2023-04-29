using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;
using System.Diagnostics;
using System.IO;

namespace OsuSkinMixer;

public partial class Splash : Control
{
    private Label UpdatingLabel;
    private Label LockFileLabel;
    private SetupPopup SetupPopup;
    private QuestionPopup ExceptionPopup;
    private TextEdit ExceptionTextEdit;
    private OkPopup UpdateCanceledPopup;
    private AnimationPlayer AnimationPlayer;

    public override void _Ready()
    {
        Settings.Log($"osu! skin mixer {Settings.VERSION} at {DateTime.Now}");

        DisplayServer.WindowSetTitle("osu! skin mixer by rednir");
        DisplayServer.WindowSetMinSize(new Vector2I(650, 300));

        UpdatingLabel = GetNode<Label>("%UpdatingLabel");
        LockFileLabel = GetNode<Label>("%LockFileLabel");
        SetupPopup = GetNode<SetupPopup>("%SetupPopup");
        ExceptionPopup = GetNode<QuestionPopup>("%ExceptionPopup");
        ExceptionTextEdit = GetNode<TextEdit>("%ExceptionTextEdit");
        UpdateCanceledPopup = GetNode<OkPopup>("%UpdateCanceledPopup");
        AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
        AnimationPlayer.AnimationFinished += OnAnimationFinished;
        UpdateCanceledPopup.PopupOut += LoadSkins;

        AnimationPlayer.Play("loading");

        Task.Run(async () =>
        {
            while (!Settings.TryCreateLockFile())
            {
                LockFileLabel.Visible = true;
                await Task.Delay(1500);
            }

            LockFileLabel.Visible = false;
            Initialise();
        })
        .ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                GD.PushError(t.Exception);
                ExceptionTextEdit.Text = t.Exception.ToString();
                ExceptionPopup.In();
            }
        });
    }

    private void Initialise()
    {
        Settings.InitialiseSettingsFile();

        if (File.Exists(Settings.AutoUpdateInstallerPath))
        {
            AutoUpdate();
            File.Delete(Settings.AutoUpdateInstallerPath);
            return;
        }

        LoadSkins();
    }

    private void AutoUpdate()
    {
        if (!Settings.Content.AutoUpdate || Settings.Content.LastVersion != Settings.VERSION)
        {
            LoadSkins();
            return;
        }

        if (Settings.Content.UpdatePending)
        {
            Settings.Content.UpdatePending = false;
            Settings.Save();

            UpdatingLabel.Visible = true;
            TryRunInstaller();
            UpdatingLabel.Visible = false;
        }
        else
        {
            UpdateCanceledPopup.In();
        }
    }

    private void TryRunInstaller()
    {
        Settings.Log($"Running installer from {Settings.AutoUpdateInstallerPath}");

        try
        {
            Process installer = Process.Start(Settings.AutoUpdateInstallerPath, "/silent");
            GetTree().Quit();
        }
        catch (Exception e)
        {
            GD.PrintErr(e);
        }

        UpdateCanceledPopup.In();
    }

    private void LoadSkins()
    {
        if (!OsuData.TryLoadSkins())
        {
            SetupPopup.In();
            SetupPopup.PopupOut += () => AnimationPlayer.Play("out");
            return;
        }

        AnimationPlayer.Play("out");
    }

    private void OnAnimationFinished(StringName animationName)
    {
        if (animationName == "out" && GetTree().ChangeSceneToFile("res://src/Main.tscn") != Error.Ok)
        {
            ExceptionTextEdit.Text = "Failed to load scene.";
            ExceptionPopup.In();
        }
    }
}
