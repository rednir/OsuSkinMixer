using Godot;
using OsuSkinMixer.Models;
using System;
using System.IO;

namespace OsuSkinMixer.Components;

public partial class CursorColourContainer : HBoxContainer
{
	public OsuSkin Skin { get; set; }

	public bool OverrideEnabled { get; private set; }

	private Label EnableOverrideLabel;
	private Button EnableOverrideButton;
	private HBoxContainer OverridingOnContainer;
	private HBoxContainer OverridingOffContainer;
	private CursorColourIcon Icon;

	public override void _Ready()
	{
		EnableOverrideLabel = GetNode<Label>("%EnableOverrideLabel");
		EnableOverrideButton = GetNode<Button>("%EnableOverrideButton");
		OverridingOnContainer = GetNode<HBoxContainer>("%OverridingOnContainer");
		OverridingOffContainer = GetNode<HBoxContainer>("%OverridingOffContainer");
		Icon = GetNode<CursorColourIcon>("%CursorColourIcon");

		EnableOverrideLabel.Text = $"Override colour for \"{Skin.Name}\"";
		EnableOverrideButton.Pressed += OnEnableOverrideButtonPressed;
	}

	private void OnEnableOverrideButtonPressed()
	{
		OverrideEnabled = true;
		OverridingOnContainer.Visible = true;
		OverridingOffContainer.Visible = false;

		InitialiseIcon();
	}

	private void InitialiseIcon()
	{
		bool isCursor2X = Skin.TryGet2XTexture("cursor", out var cursorTexture);
		bool isCursorMiddle2X = Skin.TryGet2XTexture("cursormiddle", out var cursorMiddleTexture);

		// Avoid showing the default cursormiddle if there's no custom one in the skin.
		bool showCursorMiddle = File.Exists($"{Skin.Directory.FullName}/cursormiddle.png") || !File.Exists($"{Skin.Directory.FullName}/cursor.png");

		Icon.SetValues(cursorTexture, showCursorMiddle ? cursorMiddleTexture : null);

		// TODO: this isnt working rn.
		Icon.Scale = (isCursor2X || isCursorMiddle2X) ? new Vector2(0.5f, 0.5f) : Vector2.One;
	}
}
