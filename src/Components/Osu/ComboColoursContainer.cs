using System.Dynamic;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

public partial class ComboColoursContainer : HBoxContainer
{
	public OsuSkin Skin { get; set; }

	public Color[] ComboColours { get; } = new Color[8];

	private PackedScene ComboColourIconScene;

	private HBoxContainer ContentContainer;
	private VisibleOnScreenNotifier2D VisibleOnScreenNotifier2D;

	private Texture2D HitcircleTexture;
	private Texture2D HitcircleoverlayTexture;
	private Texture2D DefaultTexture;

	private bool _isTexturesLoaded;

	public override void _Ready()
	{
		ComboColourIconScene = GD.Load<PackedScene>("res://src/Components/Osu/ComboColourIcon.tscn");

		ContentContainer = GetNode<HBoxContainer>("%ContentContainer");
		VisibleOnScreenNotifier2D = GetNode<VisibleOnScreenNotifier2D>("%VisibleOnScreenNotifier2D");
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
		ComboColours[combo] = color;

		ComboColourIcon comboColourIcon = ComboColourIconScene.Instantiate<ComboColourIcon>();
		comboColourIcon.Index = combo;
		ContentContainer.AddChild(comboColourIcon);
		comboColourIcon.SetValues(HitcircleTexture, HitcircleoverlayTexture, DefaultTexture, color);
	}
}
