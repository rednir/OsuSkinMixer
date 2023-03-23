using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class HitcircleIcon : CenterContainer
{
    private VisibleOnScreenNotifier2D VisibleOnScreenNotifier2D;
    private TextureRect HitcircleTexture;
    private TextureRect HitcircleoverlayTexture;
    private TextureRect Default1Texture;
    private TextureRect HiddenIcon;

    private OsuSkin _skin;

    private bool _isTexturesLoaded;

    public override void _Ready()
    {
        VisibleOnScreenNotifier2D = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        HitcircleTexture = GetNode<TextureRect>("HitcircleTexture");
        HitcircleoverlayTexture = GetNode<TextureRect>("HitcircleoverlayTexture");
        Default1Texture = GetNode<TextureRect>("Default1Texture");
        HiddenIcon = GetNode<TextureRect>("%HiddenIcon");

        VisibleOnScreenNotifier2D.ScreenEntered += OnScreenEntered;
    }

    public void SetSkin(OsuSkin skin)
    {
        _skin = skin;
        _isTexturesLoaded = false;

        HiddenIcon.Visible = skin.Hidden;

        string[] iniColorRgb = skin
            .SkinIni?
            .TryGetPropertyValue("Colours", "Combo2")?
            .Replace(" ", string.Empty)
            .Split(',');

        if (iniColorRgb != null
            && float.TryParse(iniColorRgb[0], out float r)
            && float.TryParse(iniColorRgb[1], out float g)
            && float.TryParse(iniColorRgb[2], out float b))
        {
            HitcircleTexture.Modulate = new Color(r / 255, g / 255, b / 255);
        }
        else
        {
            HitcircleTexture.Modulate = new Color(0, 202, 0);
        }
    }

    private void OnScreenEntered()
    {
        if (_skin == null || _isTexturesLoaded)
            return;

        _isTexturesLoaded = true;

        HitcircleTexture.Texture = _skin.Get2XTexture("hitcircle");
        HitcircleoverlayTexture.Texture = _skin.Get2XTexture("hitcircleoverlay");
        Default1Texture.Texture = _skin.Get2XTexture("default-1");
    }
}
