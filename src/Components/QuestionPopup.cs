using Godot;
using System;

namespace OsuSkinMixer.Components;

public partial class QuestionPopup : Popup
{
	public Action ConfirmAction { get; set; }

	public Action CancelAction { get; set; }

	private Button YesButton;
	private Button NoButton;

	public override void _Ready()
	{
		YesButton = GetNode<Button>("%YesButton");
		NoButton = GetNode<Button>("%NoButton");

		YesButton.Pressed += OnYes;
		NoButton.Pressed += OnNo;
	}

	private void OnYes()
	{
		Out();
		ConfirmAction?.Invoke();
	}

	private void OnNo()
	{
		Out();
		CancelAction?.Invoke();
	}
}
