using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using Image = SixLabors.ImageSharp.Image;

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
	private OkPopup ChangeColourPopup;
	private ColorPicker ColorPicker;

	public override void _Ready()
	{
		EnableOverrideLabel = GetNode<Label>("%EnableOverrideLabel");
		EnableOverrideButton = GetNode<Button>("%EnableOverrideButton");
		OverridingOnContainer = GetNode<HBoxContainer>("%OverridingOnContainer");
		OverridingOffContainer = GetNode<HBoxContainer>("%OverridingOffContainer");
		Icon = GetNode<CursorColourIcon>("%CursorColourIcon");
		ChangeColourPopup = GetNode<OkPopup>("%ChangeColourPopup");
		ColorPicker = GetNode<ColorPicker>("%ColorPicker");

		EnableOverrideLabel.Text = $"Override colour for \"{Skin.Name}\"";
		EnableOverrideButton.Pressed += OnEnableOverrideButtonPressed;
		Icon.Pressed += OnIconPressed;
		ChangeColourPopup.PopupOut += OnChangeColourPopupOut;
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

	private void OnIconPressed()
	{
		ChangeColourPopup.In();
	}

	private void OnChangeColourPopupOut()
	{
		Rgba32 rgba = new(ColorPicker.Color.R, ColorPicker.Color.G, ColorPicker.Color.B, 255);
		RecolourCursor(rgba);
	}

	private void RecolourCursor(Rgba32 target, float satThreshold = 0.1f, float satMultiplier = 1.0f)
	{
		Hsv targetHsv = ColorSpaceConverter.ToHsv(target);
		float targetHue = targetHsv.H;

		using var img = Image.Load<Rgba32>($"{Skin.Directory.FullName}/cursor.png");

		img.ProcessPixelRows(accessor =>
		{
			for (int y = 0; y < accessor.Height; y++)
			{
				var row = accessor.GetRowSpan(y);

				for (int x = 0; x < row.Length; x++)
				{
					ref Rgba32 p = ref row[x];

					var hsv = ColorSpaceConverter.ToHsv(p);

					// If pixel is too unsaturated, skip it.
					if (hsv.S <= satThreshold)
						continue;

					Hsv newHsv = new(targetHue, MathF.Min(1f, hsv.S * satMultiplier), hsv.V);

					var rgb = ColorSpaceConverter.ToRgb(newHsv);
					p = new Rgba32(
						(byte)Math.Clamp(rgb.R * 255f, 0, 255),
						(byte)Math.Clamp(rgb.G * 255f, 0, 255),
						(byte)Math.Clamp(rgb.B * 255f, 0, 255),
						p.A
					);
				}
			}
		});
		
		// TODO: unique name.
		img.SaveAsPng($"{Settings.TempFolderPath}/cursor_recolour.png");
		OS.ShellOpen($"file://{Settings.TempFolderPath}/cursor_recolour.png");
	}
}
