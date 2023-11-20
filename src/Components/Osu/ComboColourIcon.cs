namespace OsuSkinMixer.Components;

public partial class ComboColourIcon : CenterContainer
{
	public Action Pressed;

	public Color Color
    {
        get => Hitcircle.Modulate;
        set => Hitcircle.Modulate = value;
    }

	public Texture2D DefaultTexture
	{
		set => DefaultTextureRect.Texture = value;
	}

    private TextureRect Hitcircle;
	private TextureRect Hitcircleoverlay;
	private TextureRect DefaultTextureRect;
	private Button Button;

    public override void _Ready()
    {
		Hitcircle = GetNode<TextureRect>("HitcircleTexture");
		Hitcircleoverlay = GetNode<TextureRect>("HitcircleoverlayTexture");
		DefaultTextureRect = GetNode<TextureRect>("DefaultTexture");
		Button = GetNode<Button>("Button");

		Button.Pressed += OnButtonPressed;
    }

	public void SetValues(Texture2D hitcircle, Texture2D hitcircleoverlay, Texture2D defaultTexture, Color color)
	{
		Hitcircle.Modulate = color;
		Hitcircle.Texture = hitcircle;
		Hitcircleoverlay.Texture = hitcircleoverlay;
		DefaultTextureRect.Texture = defaultTexture;
	}

	private void OnButtonPressed()
	{
		Pressed?.Invoke();
	}
}
