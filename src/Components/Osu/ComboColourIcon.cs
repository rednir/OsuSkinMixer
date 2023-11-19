namespace OsuSkinMixer.Components;

public partial class ComboColourIcon : CenterContainer
{
	public int Index { get; set; }

	private TextureRect Hitcircle;
	private TextureRect Hitcircleoverlay;
	private TextureRect DefaultTexture;

    public override void _Ready()
    {
		Hitcircle = GetNode<TextureRect>("HitcircleTexture");
		Hitcircleoverlay = GetNode<TextureRect>("HitcircleoverlayTexture");
		DefaultTexture = GetNode<TextureRect>("DefaultTexture");
    }

	public void SetValues(Texture2D hitcircle, Texture2D hitcircleoverlay, Texture2D defaultTexture, Color color)
	{
		Hitcircle.Modulate = color;
		Hitcircle.Texture = hitcircle;
		Hitcircleoverlay.Texture = hitcircleoverlay;
		DefaultTexture.Texture = defaultTexture;
	}
}
