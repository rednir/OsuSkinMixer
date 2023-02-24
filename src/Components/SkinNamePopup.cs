using Godot;
using OsuSkinMixer.Statics;
using System;
using System.IO;
using System.Linq;

namespace OsuSkinMixer.Components;

public partial class SkinNamePopup : Control
{
	public Action<string> ConfirmAction { get; set; }

    private AnimationPlayer AnimationPlayer;
	private Label WarningLabel;
    private LineEdit LineEdit;
    private Button ConfirmButton;

    public override void _Ready()
    {
        AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		WarningLabel = GetNode<Label>("%WarningLabel");
        LineEdit = GetNode<LineEdit>("%LineEdit");
        ConfirmButton = GetNode<Button>("%ConfirmButton");

        ConfirmButton.Pressed += () => ConfirmAction?.Invoke(LineEdit.Text);
        LineEdit.TextSubmitted += t => ConfirmAction?.Invoke(t);
		LineEdit.TextChanged += OnTextChanged;
    }

    public void In()
		=> AnimationPlayer.Play("in");

    public void Out()
        => AnimationPlayer.Play("out");

	private void OnConfirm()
	{
		if (string.IsNullOrWhiteSpace(LineEdit.Text))
		{
			OS.Alert("Skin name cannot be empty.", "Error");
			return;
		}

		ConfirmAction?.Invoke(LineEdit.Text);
	}

	private void OnTextChanged(string text)
	{
		if (text.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
		{
			ConfirmButton.Disabled = true;
			WarningLabel.Text = "Invalid characters in skin name.";
		}
		else if (string.IsNullOrWhiteSpace(text))
		{
			ConfirmButton.Disabled = true;
			WarningLabel.Text = "Skin name cannot be empty.";
		}
		else if (OsuData.Skins.Any(s => s.Name == text))
		{
			ConfirmButton.Disabled = false;
			WarningLabel.Text = "Skin with this name already exists and will be replaced.";
		}
		else
		{
			ConfirmButton.Disabled = false;
			WarningLabel.Text = string.Empty;
		}
	}
}
