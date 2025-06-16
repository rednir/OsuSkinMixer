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

	private bool ColourChosen;

	private Label EnableOverrideLabel;
	private Button EnableOverrideButton;
	private HBoxContainer OverridingOnContainer;
	private HBoxContainer OverridingOffContainer;
	private CursorColourIcon Icon;
	private OkPopup ChangeColourPopup;
	private OkPopup OptionsPopup;
	private ColorPicker ColorPicker;
	private Button ResetButton;
	private Button OptionsButton;
	private Button IgnoreCursortrailButton;
	private Button IgnoreCursormiddleButton;
	private SpinBox SatThresholdSpinBox;

	public override void _Ready()
	{
		EnableOverrideLabel = GetNode<Label>("%EnableOverrideLabel");
		EnableOverrideButton = GetNode<Button>("%EnableOverrideButton");
		OverridingOnContainer = GetNode<HBoxContainer>("%OverridingOnContainer");
		OverridingOffContainer = GetNode<HBoxContainer>("%OverridingOffContainer");
		Icon = GetNode<CursorColourIcon>("%CursorColourIcon");
		ChangeColourPopup = GetNode<OkPopup>("%ChangeColourPopup");
		OptionsPopup = GetNode<OkPopup>("%OptionsPopup");
		ColorPicker = GetNode<ColorPicker>("%ColorPicker");
		ResetButton = GetNode<Button>("%ResetButton");
		OptionsButton = GetNode<Button>("%OptionsButton");
		IgnoreCursortrailButton = GetNode<Button>("%IgnoreCursortrailButton");
		IgnoreCursormiddleButton = GetNode<Button>("%IgnoreCursormiddleButton");
		SatThresholdSpinBox = GetNode<SpinBox>("%SatThresholdSpinBox");

		EnableOverrideLabel.Text = $"Override colour for \"{Skin.Name}\"";
		EnableOverrideButton.Pressed += OnEnableOverrideButtonPressed;
		Icon.Pressed += OnIconPressed;
		ChangeColourPopup.PopupOut += OnChangeColourPopupOut;
		OptionsPopup.PopupOut += OnOptionsPopupOut;
		ResetButton.Pressed += OnResetButtonPressed;
		OptionsButton.Pressed += OnOptionsButtonPressed;

		// TODO: TEMP
		OS.ShellOpen(Settings.TempFolderPath);
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
		Texture2D cursorTexture = Skin.Get2XTexture("cursor");
		Texture2D cursorMiddleTexture = Skin.Get2XTexture("cursormiddle");

		// Avoid showing the default cursormiddle if there's no custom one in the skin.
		bool showCursorMiddle = File.Exists($"{Skin.Directory.FullName}/cursormiddle.png") || !File.Exists($"{Skin.Directory.FullName}/cursor.png");

		Icon.SetValues(cursorTexture, showCursorMiddle ? cursorMiddleTexture : null);
	}

	private void OnIconPressed()
	{
		ChangeColourPopup.In();
	}

	private void OnResetButtonPressed()
	{
		OverrideEnabled = false;
		OverridingOnContainer.Visible = false;
		OverridingOffContainer.Visible = true;
		ColourChosen = false;
	}

	private void OnOptionsButtonPressed()
	{
		OptionsPopup.In();
	}

	private void OnChangeColourPopupOut()
		=> UpdateIconColour();

	private void OnOptionsPopupOut()
	{
		// If the user hasn't chosen a colour yet there's no point updating anything.
		if (ColourChosen)
			UpdateIconColour();
	}

//"CHANGE CURSORTRAIL CURSORMIDDLE AND 2X"
	private void UpdateIconColour()
	{
		string tempCursorPath = $"{Settings.TempFolderPath}/cursor_recolour.png";

		Rgba32 rgba = new(ColorPicker.Color.R, ColorPicker.Color.G, ColorPicker.Color.B, 255);
		RecolourCursor(tempCursorPath, rgba, (float)SatThresholdSpinBox.Value);

		Godot.Image newCursorImage = new();
		var err = newCursorImage.Load(tempCursorPath);

		if (err != Error.Ok)
		{
			// TODO: idk
			throw new Exception();
		}
		//"why is it not changing the color in preview" ----
		// TODO: not null cursormiddle, change the colour of this too.
		// TODO: and also cursor trail?
		Icon.SetValues(ImageTexture.CreateFromImage(newCursorImage), null);

		// When the user presses the override button, the icon will appear unchanged until a colour is chosen.
		ColourChosen = true;
	}

	private void RecolourCursor(string output, Rgba32 target, float satThreshold, float nonBlackThreshold = 0.06f)
	{
		Hsv targetHsv = ColorSpaceConverter.ToHsv(target);

		using var img = Image.Load<Rgba32>($"{Skin.Directory.FullName}/cursor.png");

		bool isCursorGreyscale = CheckGreyscaleCursor(img);

		img.ProcessPixelRows(accessor =>
		{
			for (int y = 0; y < accessor.Height; y++)
			{
				var row = accessor.GetRowSpan(y);

				for (int x = 0; x < row.Length; x++)
				{
					ref Rgba32 p = ref row[x];

					var hsv = ColorSpaceConverter.ToHsv(p);

					if (isCursorGreyscale)
					{
						// Ignore very dark pixels for greyscale cursors, as they are likely to be shadows or outlines.
						if (hsv.V < nonBlackThreshold)
							continue;
					}
					else
					{
						// Ignore unsaturated pixels for cursors with saturated colours.
						if (hsv.S < satThreshold)
							continue;
					}

					Hsv newHsv = new(targetHsv.H, targetHsv.S, targetHsv.V);

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

		img.SaveAsPng(output);
	}

	private static bool CheckGreyscaleCursor(Image<Rgba32> img, float threshold = 0.03f)
	{
		long saturationSum = 0;
		long pixelCount = 0;

		img.ProcessPixelRows(accessor =>
		{
			for (int y = 0; y < accessor.Height; y++)
			{
				var row = accessor.GetRowSpan(y);

				for (int x = 0; x < row.Length; x++)
				{
					var p = row[x];
					if (p.A == 0)
						continue;

					saturationSum += (long)(ColorSpaceConverter.ToHsv(p).S * 1000);
					pixelCount++;
				}
			}
		});

		if (pixelCount == 0)
			return false;
		
		return (saturationSum / (float)pixelCount) < (threshold * 1000);
	}
}
