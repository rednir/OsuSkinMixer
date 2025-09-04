namespace OsuSkinMixer;

using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;
using System.Diagnostics;
using System.IO;

public partial class Splash : Control
{
    private Label UpdatingLabel;
    private Label LockFileLabel;
    private SetupPopup SetupPopup;
    private QuestionPopup ExceptionPopup;
    private TextEdit ExceptionTextEdit;
    private OkPopup UpdateCanceledPopup;
    private QuestionPopup UpdateQuestionPopup;
    private Toast Toast;
    private AnimationPlayer AnimationPlayer;

    public override void _Ready()
    {
        Settings.Log($"osu! skin mixer {Settings.VERSION} at {DateTime.Now}");

        UpdatingLabel = GetNode<Label>("%UpdatingLabel");
        LockFileLabel = GetNode<Label>("%LockFileLabel");
        SetupPopup = GetNode<SetupPopup>("%SetupPopup");
        ExceptionPopup = GetNode<QuestionPopup>("%ExceptionPopup");
        ExceptionTextEdit = GetNode<TextEdit>("%ExceptionTextEdit");
        UpdateCanceledPopup = GetNode<OkPopup>("%UpdateCanceledPopup");
        UpdateQuestionPopup = GetNode<QuestionPopup>("%UpdateQuestionPopup");
        Toast = GetNode<Toast>("%Toast");
        AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");

        AnimationPlayer.AnimationFinished += OnAnimationFinished;
        UpdateCanceledPopup.PopupOut += LoadSkins;
        UpdateQuestionPopup.CancelAction += LoadSkins;
        UpdateQuestionPopup.ConfirmAction += TryRunInstaller;

        AnimationPlayer.Play("loading");

        Task.Run(async () =>
        {
            GodotThread.SetThreadSafetyChecksEnabled(false);
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
                ExceptionTextEdit.SetDeferred(TextEdit.PropertyName.Text, t.Exception.ToString());
                ExceptionPopup.In();
            }
        });
    }

    private void Initialise()
    {
        Settings.InitialiseSettingsFile();

        AudioServer.SetBusMute(AudioServer.GetBusIndex("Master"), Settings.Content.VolumeMute);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), (float)Settings.Content.Volume);

        if (File.Exists(Settings.AutoUpdateInstallerPath))
        {
            AutoUpdate();
            return;
        }

        LoadSkins();
    }

    private void AutoUpdate()
    {
        if (!Settings.Content.AutoUpdate || Settings.Content.LastVersion != Settings.VERSION)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(Settings.TempFolderPath))
                    File.Delete(file);
            }
            catch (Exception e)
            {
                GD.PrintErr(e);
                Toast.Push($"Failed to clean up temporary files: {e.Message}");
                Task.Delay(3000).Wait();
            }

            LoadSkins();
            return;
        }

        if (Settings.Content.UpdatePending)
        {
            UpdateQuestionPopup.In();
        }
        else
        {
            UpdateCanceledPopup.In();
        }
    }

    private void TryRunInstaller()
    {
        UpdatingLabel.Visible = true;
        Settings.Content.UpdatePending = false;
        Settings.Save();
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
        UpdatingLabel.Visible = false;
    }

    private void LoadSkins()
    {
        if (!OsuData.TryLoadSkins())
        {
            SetupPopup.In();
            SetupPopup.PopupOut += () => AnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "out");
            return;
        }

        AnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "out");
    }

    private void OnAnimationFinished(StringName animationName)
    {
        DisplayServer.WindowSetTitle("osu! skin mixer");
        DisplayServer.WindowSetMinSize(new Vector2I(650, 300));

        if (animationName == "out" && GetTree().ChangeSceneToFile("res://src/Main.tscn") != Error.Ok)
        {
            ExceptionTextEdit.Text = "Failed to load scene.";
            ExceptionPopup.In();
        }
    }
}
