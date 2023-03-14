using Godot;
using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OsuSkinMixer;

public partial class Splash : Control
{
	private Label UpdatingLabel;
	private SetupPopup SetupPopup;
	private QuestionPopup ExceptionPopup;
	private TextEdit ExceptionTextEdit;
	private AnimationPlayer AnimationPlayer;

	public override void _Ready()
	{
		Settings.Log($"osu! skin mixer {Settings.VERSION} at {DateTime.Now}");

		DisplayServer.WindowSetTitle("osu! skin mixer by rednir");
		DisplayServer.WindowSetMinSize(new Vector2I(650, 300));

		UpdatingLabel = GetNode<Label>("%UpdatingLabel");
		SetupPopup = GetNode<SetupPopup>("%SetupPopup");
		ExceptionPopup = GetNode<QuestionPopup>("%ExceptionPopup");
		ExceptionTextEdit = GetNode<TextEdit>("%ExceptionTextEdit");
		AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		AnimationPlayer.AnimationFinished += OnAnimationFinished;

		AnimationPlayer.Play("loading");

		Task.Run(Initialise)
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
			if (Settings.Content.LastVersion == Settings.VERSION)
			{
				// Try run installer, if it succeeds execution should stop here.
				UpdatingLabel.Visible = true;
				TryRunInstaller();
			}

			// Update finished, clean up installer.
			File.Delete(Settings.AutoUpdateInstallerPath);
		}

		if (!OsuData.TryLoadSkins())
		{
			SetupPopup.In();
			SetupPopup.PopupOut += () => AnimationPlayer.Play("out");
			return;
		}

		AnimationPlayer.Play("out");
	}

	private static void TryRunInstaller()
	{
		Settings.Log($"Running installer from {Settings.AutoUpdateInstallerPath}");
		Process installer = Process.Start(Settings.AutoUpdateInstallerPath);
		installer.WaitForExit();
	}

	private void OnAnimationFinished(StringName animationName)
	{
		if (animationName == "out")
		{
			if (GetTree().ChangeSceneToFile("res://src/Main.tscn") != Error.Ok)
				OS.Alert("Error");
		}
	}
}
