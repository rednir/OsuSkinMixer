using System.Dynamic;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

public partial class ComboColoursContainer : HBoxContainer
{
	public OsuSkin Skin { get; set; }

	public ComboColourIcon[] ComboColourIcons { get; } = new ComboColourIcon[8];

	private int SelectedComboIndex = -1;

	private PackedScene ComboColourIconScene;

	private OkPopup ChangeColorPopup;
	private ColorPicker ColorPicker;
	private Button RemoveColourButton;
	private HBoxContainer ContentContainer;
	private Button ResetButton;
	private Button AddButton;
	private VisibleOnScreenNotifier2D VisibleOnScreenNotifier2D;

	private Texture2D HitcircleTexture;
	private Texture2D HitcircleoverlayTexture;

	private bool _isTexturesLoaded;

	public override void _Ready()
	{
		ComboColourIconScene = GD.Load<PackedScene>("res://src/Components/Osu/ComboColourIcon.tscn");

		ChangeColorPopup = GetNode<OkPopup>("%ChangeColorPopup");
		ColorPicker = GetNode<ColorPicker>("%ColorPicker");
		RemoveColourButton = GetNode<Button>("%RemoveColourButton");
		ContentContainer = GetNode<HBoxContainer>("%ContentContainer");
		ResetButton = GetNode<Button>("%ResetButton");
		AddButton = GetNode<Button>("%AddButton");
		VisibleOnScreenNotifier2D = GetNode<VisibleOnScreenNotifier2D>("%VisibleOnScreenNotifier2D");

		ColorPicker.ColorChanged += OnColorPickerColorChanged;
		RemoveColourButton.Pressed += OnRemoveColourButtonPressed;
		ResetButton.Pressed += OnResetButtonPressed;
		AddButton.Pressed += OnAddButtonPressed;
		VisibleOnScreenNotifier2D.ScreenEntered += OnScreenEntered;
	}

	private void OnScreenEntered()
    {
        if (Skin == null || _isTexturesLoaded)
            return;

        _isTexturesLoaded = true;

        HitcircleTexture = Skin.Get2XTexture("hitcircle");
        HitcircleoverlayTexture = Skin.Get2XTexture("hitcircleoverlay");

		InitialiseComboColours();
    }

	private void InitialiseComboColours()
	{
		for (int i = 0; i < 8; i++)
		{
			string[] iniColorRgb = Skin
				.SkinIni?
				.TryGetPropertyValue("Colours", $"Combo{i + 1}")?
				.Replace(" ", string.Empty)
				.Split(',');

			if (iniColorRgb != null
				&& float.TryParse(iniColorRgb[0], out float r)
				&& float.TryParse(iniColorRgb[1], out float g)
				&& float.TryParse(iniColorRgb[2], out float b))
			{
				Color color = new (r / 255, g / 255, b / 255);
				AddComboColour(i, color);

				// If all 8 combo colours are used, don't allow adding more.
				if (i == 7)
					AddButton.Disabled = true;
			}
		}
	}

	private void AddComboColour(int index, Color color)
	{
		string hitcirclePrefix = Skin.SkinIni.TryGetPropertyValue("Fonts", "HitCirclePrefix") ?? "default";
		Texture2D defaultTexture = Skin.Get2XTexture($"{hitcirclePrefix}-{index + 1}");

		ComboColourIcon comboColourIcon = ComboColourIconScene.Instantiate<ComboColourIcon>();
		ContentContainer.AddChild(comboColourIcon);
		comboColourIcon.Pressed += () => OnComboColourIconPressed(index);
		comboColourIcon.SetValues(HitcircleTexture, HitcircleoverlayTexture, defaultTexture, color);
	
		ComboColourIcons[index] = comboColourIcon;
	}

	private void OnComboColourIconPressed(int index)
	{
		SelectedComboIndex = index;

		ColorPicker.Color = ComboColourIcons[index].Color;
		ChangeColorPopup.In();
	}
	
	private void OnColorPickerColorChanged(Color color)
	{
		ComboColourIcons[SelectedComboIndex].Color = color;
	}

	private void OnRemoveColourButtonPressed()
	{
		ComboColourIcons[SelectedComboIndex]?.QueueFree();

		// Move down all combo colours that follow the removed one.
		for (int i = SelectedComboIndex; i < 7; i++)
		{
			ComboColourIcons[i] = ComboColourIcons[i + 1];
			ComboColourIcons[i + 1] = null;
		}

		AddButton.Disabled = false;
		ChangeColorPopup.Out();
	}

	private void OnResetButtonPressed()
	{
		for (int i = 0; i < 8; i++)
		{
			ComboColourIcons[i]?.QueueFree();
			ComboColourIcons[i] = null;
		}

		InitialiseComboColours();
	}

	private void OnAddButtonPressed()
	{
		for (int i = 0; i < 8; i++)
		{
			if (ComboColourIcons[i] != null)
				continue;

			AddComboColour(i, new Color(0.75f, 0.75f, 0.75f));

			if (i == 7)
				AddButton.Disabled = true;

			return;
		}
	}
}
