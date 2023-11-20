using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class ComboColoursContainer : HBoxContainer
{
	public OsuSkin Skin { get; set; }

	public List<ComboColourIcon> ComboColourIcons { get; } = new();

	public bool IsModified { get; set; }

	private ComboColourIcon SelectedComboColourIcon;

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

	public void Reset()
	{
		_isTexturesLoaded = false;
		OnScreenEntered();
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
		ComboColourIcons.Clear();
		IsModified = false;

		foreach (Node node in ContentContainer.GetChildren())
			node.QueueFree();

		for (int i = 0; i < 8; i++)
		{
			string[] iniColorRgb = Skin
				.SkinIni?
				.TryGetPropertyValue("Colours", $"Combo{i + 1}")?
				.Replace(" ", string.Empty)
				.Split(',');

			if (iniColorRgb == null)
			{
				// Assume we reached the end of the combo colour list.
				AddButton.Disabled = false;
				return;
			}

			if (iniColorRgb != null
				&& float.TryParse(iniColorRgb[0], out float r)
				&& float.TryParse(iniColorRgb[1], out float g)
				&& float.TryParse(iniColorRgb[2], out float b))
			{
				Color color = new(r / 255, g / 255, b / 255);
				AddComboColour(color);
			}
		}

		// If all 8 combo colours are used, don't allow adding more.
		AddButton.Disabled = true;
	}

	private void AddComboColour(Color color)
	{
		ComboColourIcon comboColourIcon = ComboColourIconScene.Instantiate<ComboColourIcon>();
		ContentContainer.AddChild(comboColourIcon);
		comboColourIcon.Pressed += () => OnComboColourIconPressed(comboColourIcon);
		comboColourIcon.SetValues(HitcircleTexture, HitcircleoverlayTexture, GetDefaultTexture(ComboColourIcons.Count), color);
		ComboColourIcons.Add(comboColourIcon);
	}

	private void UpdateDefaultTextures()
	{
		for (int i = 0; i < ComboColourIcons.Count; i++)
			ComboColourIcons[i].DefaultTexture = GetDefaultTexture(i);
	}

	private Texture2D GetDefaultTexture(int comboNumber)
	{
		string hitcirclePrefix = Skin.SkinIni.TryGetPropertyValue("Fonts", "HitCirclePrefix") ?? "default";
		return Skin.Get2XTexture($"{hitcirclePrefix}-{comboNumber}");
	}

	private void OnComboColourIconPressed(ComboColourIcon comboColourIcon)
	{
		SelectedComboColourIcon = comboColourIcon;
		ColorPicker.Color = comboColourIcon.Color;
		ChangeColorPopup.In();
	}

	private void OnColorPickerColorChanged(Color color)
	{
		IsModified = true;
		SelectedComboColourIcon.Color = color;
	}

	private void OnRemoveColourButtonPressed()
	{
		IsModified = true;

		SelectedComboColourIcon.QueueFree();
		ComboColourIcons.Remove(SelectedComboColourIcon);

		AddButton.Disabled = false;
		UpdateDefaultTextures();
		ChangeColorPopup.Out();
	}

	private void OnResetButtonPressed()
	{
		foreach (ComboColourIcon icon in ComboColourIcons)
			icon.QueueFree();

		InitialiseComboColours();
	}

	private void OnAddButtonPressed()
	{
		IsModified = true;
		AddComboColour(new Color(0.75f, 0.75f, 0.75f));

		if (ComboColourIcons.Count == 8)
			AddButton.Disabled = true;
	}
}
