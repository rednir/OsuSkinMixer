using Godot;
using OsuSkinMixer.Components;
using OsuSkinMixer.Statics;
using System;
using System.Threading.Tasks;

namespace OsuSkinMixer;

public partial class Splash : Control
{
	private SetupPopup SetupPopup;
	private QuestionPopup ExceptionPopup;
	private TextEdit ExceptionTextEdit;
	private AnimationPlayer AnimationPlayer;

	public override void _Ready()
	{
		Settings.Log($"osu! skin mixer {Settings.VERSION} at {DateTime.Now}");

		DisplayServer.WindowSetTitle("osu! skin mixer by rednir");
		DisplayServer.WindowSetMinSize(new Vector2I(600, 300));

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
		if (animationName == "out")
		{
			if (GetTree().ChangeSceneToFile("res://src/Main.tscn") != Error.Ok)
				OS.Alert("Error");
		}
	}
}
