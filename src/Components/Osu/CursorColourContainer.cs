using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;
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

// TODO: reuse code from combo colours container
public partial class CursorColourContainer : HBoxContainer
{
	public OsuSkinBase Skin { get; set; }

	public bool OverrideEnabled { get; private set; }

	public bool IsColourChosen { get; private set; }

	public event Action OverrideStateChanged;

	// When modifying starts, the skin machine will copy everything in this directory to the target skin's directory.
	public string GeneratedImagesDirPath => $"{Settings.DeleteOnExitFolderPath}/cc_{Skin.Name}";

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
	}

	private void OnEnableOverrideButtonPressed()
	{
		try
		{
			// We're doing a little I/O, so just being safe with the try-catch.
			InitialiseIcon();
		}
		catch (Exception e)
		{
			Settings.PushException(e);
		}

		OverrideEnabled = true;
		OverrideStateChanged?.Invoke();
		OverridingOnContainer.Visible = true;
		OverridingOffContainer.Visible = false;
	}

	private void InitialiseIcon()
	{
		// TODO: lazer
		// Texture2D cursorTexture = Skin.Get2XTexture("cursor");
		// Texture2D cursorMiddleTexture = Skin.Get2XTexture("cursormiddle");

		// // Avoid showing the default cursormiddle if there's no custom one in the skin.
		// bool showCursorMiddle = File.Exists($"{Skin.Directory.FullName}/cursormiddle.png") || !File.Exists($"{Skin.Directory.FullName}/cursor.png");

		// Icon.SetValues(cursorTexture, showCursorMiddle ? cursorMiddleTexture : null);

		// Directory.CreateDirectory(GeneratedImagesDirPath);
	}

	private void OnIconPressed()
	{
		ChangeColourPopup.In();
	}

	private void OnResetButtonPressed()
	{
		OverrideEnabled = false;
		OverrideStateChanged?.Invoke();
		OverridingOnContainer.Visible = false;
		OverridingOffContainer.Visible = true;
		IsColourChosen = false;
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
		if (IsColourChosen)
			UpdateIconColour();
	}

	private void UpdateIconColour()
	{
		// Rgba32 rgba = new(ColorPicker.Color.R, ColorPicker.Color.G, ColorPicker.Color.B, 255);

		// string[] filesToRecolour =
		// [
		// 	"cursor",
		// 	IgnoreCursormiddleButton.ButtonPressed ? null : "cursormiddle",
		// 	IgnoreCursortrailButton.ButtonPressed ? null : "cursortrail"
		// ];

		// // The user may have chosen to ignore the cursormiddle or cursortrail, so clean up files just in this case.
		// foreach (string file in Directory.EnumerateFiles(GeneratedImagesDirPath))
		// 	File.Delete(file);

		// foreach (string file in filesToRecolour)
		// {
		// 	if (file is null)
		// 		continue;

		// 	RecolourImage($"{Skin.Directory}/{file}.png", $"{GeneratedImagesDirPath}/{file}.png", rgba, (float)SatThresholdSpinBox.Value);
		// 	RecolourImage($"{Skin.Directory}/{file}@2x.png", $"{GeneratedImagesDirPath}/{file}@2x.png", rgba, (float)SatThresholdSpinBox.Value);
		// }

		// Godot.Image cursorImage = null;
		// Godot.Image cursormiddleImage = null;

		// if (File.Exists($"{GeneratedImagesDirPath}/cursor.png"))
		// {
		// 	cursorImage = new Godot.Image();
		// 	cursorImage.Load($"{GeneratedImagesDirPath}/cursor.png");
		// }


		// if (File.Exists($"{GeneratedImagesDirPath}/cursormiddle.png"))
		// {
		// 	cursormiddleImage = new Godot.Image();
		// 	cursormiddleImage.Load($"{GeneratedImagesDirPath}/cursormiddle.png");
		// }

		// Icon.SetValues(
		// 	cursorImage is not null ? ImageTexture.CreateFromImage(cursorImage) : null,
		// 	cursormiddleImage is not null ? ImageTexture.CreateFromImage(cursormiddleImage) : null);

		// // When the user presses the override button, the icon will appear unchanged until a colour is chosen.
		// IsColourChosen = true;
	}

	private static void RecolourImage(string input, string output, Rgba32 target, float satThreshold, float nonBlackThreshold = 0.06f)
	{
		// Silently return if the input file doesn't exist. Some skins don't skin some of these elements.
		if (input is null || !File.Exists(input))
			return;

		using var img = Image.Load<Rgba32>(input);

		Hsv targetHsv = ColorSpaceConverter.ToHsv(target);
		bool isCursorGreyscale = CheckImageIsMostlyGreyscale(img);

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

	private static bool CheckImageIsMostlyGreyscale(Image<Rgba32> img, float threshold = 0.03f)
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
