using System.Dynamic;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

public partial class ComboColoursContainer : HBoxContainer
{
	public OsuSkin Skin { get; set; }

	public ComboColourIcon[] ComboColourIcons { get; } = new ComboColourIcon[8];

	private int SelectedCombo = -1;

	private PackedScene ComboColourIconScene;

	private OkPopup ChangeColorPopup;
	private ColorPicker ColorPicker;
	private HBoxContainer ContentContainer;
	private VisibleOnScreenNotifier2D VisibleOnScreenNotifier2D;

	private Texture2D HitcircleTexture;
	private Texture2D HitcircleoverlayTexture;
	private Texture2D DefaultTexture;

	private bool _isTexturesLoaded;

	public override void _Ready()
	{
		ComboColourIconScene = GD.Load<PackedScene>("res://src/Components/Osu/ComboColourIcon.tscn");

		ChangeColorPopup = GetNode<OkPopup>("%ChangeColorPopup");
		ColorPicker = GetNode<ColorPicker>("%ColorPicker");
		ContentContainer = GetNode<HBoxContainer>("%ContentContainer");
		VisibleOnScreenNotifier2D = GetNode<VisibleOnScreenNotifier2D>("%VisibleOnScreenNotifier2D");

		ColorPicker.ColorChanged += OnColorPickerColorChanged;
		VisibleOnScreenNotifier2D.ScreenEntered += OnScreenEntered;
	}

	private void OnScreenEntered()
    {
        if (Skin == null || _isTexturesLoaded)
            return;

        _isTexturesLoaded = true;

        string hitcirclePrefix = Skin.SkinIni.TryGetPropertyValue("Fonts", "HitCirclePrefix") ?? "default";

        HitcircleTexture = Skin.Get2XTexture("hitcircle");
        HitcircleoverlayTexture = Skin.Get2XTexture("hitcircleoverlay");
        DefaultTexture = Skin.Get2XTexture($"{hitcirclePrefix}-1");

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
			}
		}
	}

	private void AddComboColour(int combo, Color color)
	{
		ComboColourIcon comboColourIcon = ComboColourIconScene.Instantiate<ComboColourIcon>();
		ContentContainer.AddChild(comboColourIcon);
		comboColourIcon.Pressed += () => OnComboColourIconPressed(combo);
		comboColourIcon.SetValues(HitcircleTexture, HitcircleoverlayTexture, DefaultTexture, color);
	
		ComboColourIcons[combo] = comboColourIcon;
	}

	private void OnComboColourIconPressed(int combo)
	{
		SelectedCombo = combo;

		ColorPicker.Color = ComboColourIcons[combo].Color;
		ChangeColorPopup.In();
	}
	
	private void OnColorPickerColorChanged(Color color)
	{
		ComboColourIcons[SelectedCombo].Color = color;
	}
}
