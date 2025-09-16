using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;

namespace OsuSkinMixer.Components;

public partial class ComboColoursContainer : HBoxContainer
{
	public OsuSkinBase Skin { get; set; }

	public IEnumerable<ComboColourIcon> ComboColourIcons => ContentContainer.GetChildren()
		.Where(c => !c.IsQueuedForDeletion()).Cast<ComboColourIcon>();

	public bool OverrideEnabled { get; private set; }

	public event Action OverrideStateChanged;

	private ComboColourIcon SelectedComboColourIcon;

	private PackedScene ComboColourIconScene;

	private HBoxContainer OverridingOnContainer;
	private HBoxContainer OverridingOffContainer;
	private Label EnableOverrideLabel;
	private Button EnableOverrideButton;
	private OkPopup ChangeColorPopup;
	private ColorPicker ColorPicker;
	private Button RemoveColourButton;
	private HBoxContainer ContentContainer;
	private Button ResetButton;
	private Button AddButton;

	private Texture2D HitcircleTexture;
	private Texture2D HitcircleoverlayTexture;

	public override void _Ready()
	{
		ComboColourIconScene = GD.Load<PackedScene>("res://src/Components/Osu/ComboColourIcon.tscn");

		OverridingOnContainer = GetNode<HBoxContainer>("%OverridingOnContainer");
		OverridingOffContainer = GetNode<HBoxContainer>("%OverridingOffContainer");
		EnableOverrideLabel = GetNode<Label>("%EnableOverrideLabel");
		EnableOverrideButton = GetNode<Button>("%EnableOverrideButton");
		ChangeColorPopup = GetNode<OkPopup>("%ChangeColorPopup");
		ColorPicker = GetNode<ColorPicker>("%ColorPicker");
		RemoveColourButton = GetNode<Button>("%RemoveColourButton");
		ContentContainer = GetNode<HBoxContainer>("%ContentContainer");
		ResetButton = GetNode<Button>("%ResetButton");
		AddButton = GetNode<Button>("%AddButton");

		EnableOverrideButton.Pressed += OnEnableOverrideButtonPressed;
		ColorPicker.ColorChanged += OnColorPickerColorChanged;
		RemoveColourButton.Pressed += OnRemoveColourButtonPressed;
		ResetButton.Pressed += OnResetButtonPressed;
		AddButton.Pressed += OnAddButtonPressed;

		EnableOverrideLabel.Text = $"Override colours for \"{Skin.Name}\"";
	}

	public void Reset()
		=> OnResetButtonPressed();

	private void OnEnableOverrideButtonPressed()
	{
		OverrideEnabled = true;
		OverrideStateChanged?.Invoke();
		OverridingOnContainer.Visible = true;
		OverridingOffContainer.Visible = false;

		InitialiseComboColours();
	}

	private void LoadTextures()
	{
		// TODO: lazer
		// HitcircleTexture = Skin.Get2XTexture("hitcircle");
		// HitcircleoverlayTexture = Skin.Get2XTexture("hitcircleoverlay");
	}

	private void InitialiseComboColours()
	{
		LoadTextures();

		foreach (Node node in ComboColourIcons)
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
				break;
			}

			if (iniColorRgb != null
				&& float.TryParse(iniColorRgb[0], out float r)
				&& float.TryParse(iniColorRgb[1], out float g)
				&& float.TryParse(iniColorRgb[2], out float b))
			{
				Color color = new(r / 255, g / 255, b / 255);
				AddComboColour(color);
			}

			if (i == 7)
			{
				// If all 8 combo colours are used, don't allow adding more.
				AddButton.Disabled = true;
			}
		}

		if (!ComboColourIcons.Any())
			return;

		// The icon representing Combo1 should be shown as the last combo colour.
		ContentContainer.MoveChild(ComboColourIcons.First(), ContentContainer.GetChildCount() - 1);
		UpdateDefaultTextures();
	}

	private void AddComboColour(Color color)
	{
		ComboColourIcon comboColourIcon = ComboColourIconScene.Instantiate<ComboColourIcon>();
		ContentContainer.AddChild(comboColourIcon);
		comboColourIcon.Pressed += () => OnComboColourIconPressed(comboColourIcon);
		comboColourIcon.SetValues(HitcircleTexture, HitcircleoverlayTexture, GetDefaultTexture(ComboColourIcons.Count()), color);
	}

	private void UpdateDefaultTextures()
	{
		int i = 1;
		foreach (ComboColourIcon icon in ComboColourIcons)
		{
			icon.DefaultTexture = GetDefaultTexture(i);
			i++;
		}
	}

	private Texture2D GetDefaultTexture(int comboNumber)
	{
		// TODO: lazer
		// string hitcirclePrefix = Skin.SkinIni.TryGetPropertyValue("Fonts", "HitCirclePrefix") ?? "default";
		// return Skin.Get2XTexture($"{hitcirclePrefix}-{comboNumber}");
		return null;
	}

	private void OnComboColourIconPressed(ComboColourIcon comboColourIcon)
	{
		SelectedComboColourIcon = comboColourIcon;
		ColorPicker.Color = comboColourIcon.Color;
		ChangeColorPopup.In();
	}

	private void OnColorPickerColorChanged(Color color)
	{
		SelectedComboColourIcon.Color = color;
	}

	private void OnRemoveColourButtonPressed()
	{
		SelectedComboColourIcon.QueueFree();

		AddButton.Disabled = false;
		UpdateDefaultTextures();
		ChangeColorPopup.Out();
	}

	private void OnResetButtonPressed()
	{
		foreach (ComboColourIcon icon in ComboColourIcons)
			icon.QueueFree();

		OverrideEnabled = false;
		OverrideStateChanged?.Invoke();
		OverridingOnContainer.Visible = false;
		OverridingOffContainer.Visible = true;
	}

	private void OnAddButtonPressed()
	{
		AddComboColour(new Color(0.75f, 0.75f, 0.75f));

		if (ComboColourIcons.Count() == 8)
			AddButton.Disabled = true;
	}
}
