namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;

public partial class HitcircleIcon : CenterContainer
{
    private VisibleOnScreenNotifier2D VisibleOnScreenNotifier2D;
    private CanvasGroup HitcircleGroup;
    private Sprite2D HitcircleSprite;
    private Sprite2D HitcircleoverlaySprite;
    private Sprite2D Default1Sprite;
    private TextureRect HiddenIcon;

    private OsuSkin _skin;

    private bool _isTexturesLoaded;

    public override void _Ready()
    {
        VisibleOnScreenNotifier2D = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        HitcircleGroup = GetNode<CanvasGroup>("%HitcircleGroup");
        HitcircleSprite = GetNode<Sprite2D>("%HitcircleSprite");
        HitcircleoverlaySprite = GetNode<Sprite2D>("%HitcircleoverlaySprite");
        Default1Sprite = GetNode<Sprite2D>("%Default1Sprite");
        HiddenIcon = GetNode<TextureRect>("%HiddenIcon");

        VisibleOnScreenNotifier2D.ScreenEntered += OnScreenEntered;
    }

    public void SetSkin(OsuSkin skin)
    {
        _skin = skin;
        _isTexturesLoaded = false;

        HiddenIcon.SetDeferred(PropertyName.Visible, skin.Hidden);

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
            HitcircleSprite.SetDeferred(PropertyName.Modulate, new Color(r / 255, g / 255, b / 255));
        }
        else
        {
            HitcircleSprite.SetDeferred(PropertyName.Modulate, new Color(0, 202, 0));
        }
    }

    private void OnScreenEntered()
    {
        if (_skin is null || _isTexturesLoaded)
            return;

        if (_skin?.Directory is null)
        {
            Modulate = new Color(1, 1, 1, 0.25f);
            return;
        }

        _isTexturesLoaded = true;

        string hitcirclePrefix = _skin.SkinIni.TryGetPropertyValue("Fonts", "HitCirclePrefix") ?? "default";

        Task.Run(() => 
        {
            // Scale textures based on whether they are @2x or not.
            if (_skin.TryGet2XTexture($"{hitcirclePrefix}-1", out var default1) &&
                _skin.TryGet2XTexture("hitcircle", out var hitcircle) &&
                _skin.TryGet2XTexture("hitcircleoverlay", out var hitcircleoverlay))
            {
                HitcircleGroup.SetDeferred(CanvasGroup.PropertyName.Scale, new Vector2(0.5f, 0.5f));
                Default1Sprite.SetDeferred(Sprite2D.PropertyName.Texture, default1);
                HitcircleSprite.SetDeferred(Sprite2D.PropertyName.Texture, hitcircle);
                HitcircleoverlaySprite.SetDeferred(Sprite2D.PropertyName.Texture, hitcircleoverlay);
            }
            else
            {
                Default1Sprite.SetDeferred(Sprite2D.PropertyName.Texture, _skin.GetTexture($"{hitcirclePrefix}-1"));
                HitcircleSprite.SetDeferred(Sprite2D.PropertyName.Texture, _skin.GetTexture("hitcircle"));
                HitcircleoverlaySprite.SetDeferred(Sprite2D.PropertyName.Texture, _skin.GetTexture("hitcircleoverlay"));
            }
        });
    }
}
