namespace OsuSkinMixer.Components;

using System.IO;
using OsuSkinMixer.Autoload;
using OsuSkinMixer.Models;

public partial class HitcircleIcon : CenterContainer
{
    private TextureLoadingService TextureLoadingService;
    private VisibleOnScreenNotifier2D VisibleOnScreenNotifier2D;
    private CanvasGroup HitcircleGroup;
    private Sprite2D HitcircleSprite;
    private Sprite2D HitcircleoverlaySprite;
    private Sprite2D Default1Sprite;
    private TextureRect HiddenIcon;
    private AnimationPlayer LoadingAnimationPlayer;

    private OsuSkin _skin;

    private bool _isTexturesLoaded;

    private string _hitcirclePrefix;

    public override void _Ready()
    {
        TextureLoadingService = GetNode<TextureLoadingService>("/root/TextureLoadingService");
        VisibleOnScreenNotifier2D = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        HitcircleGroup = GetNode<CanvasGroup>("%HitcircleGroup");
        HitcircleSprite = GetNode<Sprite2D>("%HitcircleSprite");
        HitcircleoverlaySprite = GetNode<Sprite2D>("%HitcircleoverlaySprite");
        Default1Sprite = GetNode<Sprite2D>("%Default1Sprite");
        HiddenIcon = GetNode<TextureRect>("%HiddenIcon");
        LoadingAnimationPlayer = GetNode<AnimationPlayer>("%LoadingAnimationPlayer");

        TextureLoadingService.TextureReady += OnTextureReady;
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

    private void OnTextureReady(string filepath, Texture2D texture, bool is2x)
    {
        if (!IsInstanceValid(this) || _skin is null)
            return;

        if (filepath == _skin.GetElementFilepathWithoutExtension("hitcircle"))
        {
            HitcircleSprite.SetDeferred(Sprite2D.PropertyName.Texture, texture);
        }
        else if (filepath == _skin.GetElementFilepathWithoutExtension($"{_hitcirclePrefix}-1"))
        {
            Default1Sprite.SetDeferred(Sprite2D.PropertyName.Texture, texture);
        }
        else if (filepath == _skin.GetElementFilepathWithoutExtension("hitcircleoverlay"))
        {
            HitcircleoverlaySprite.SetDeferred(Sprite2D.PropertyName.Texture, texture);
            LoadingAnimationPlayer.Play("out");
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

        // TODO: this is duplicated from the hitcircle class.
        bool _use2x = File.Exists($"{_skin.Directory}/approachcircle@2x.png")
            && File.Exists($"{_skin.Directory}/hitcircle@2x.png")
            && File.Exists($"{_skin.Directory}/hitcircleoverlay@2x.png");

        if (_use2x)
            HitcircleGroup.SetDeferred(CanvasGroup.PropertyName.Scale, new Vector2(0.5f, 0.5f));

        _hitcirclePrefix = _skin.SkinIni.TryGetPropertyValue("Fonts", "HitCirclePrefix") ?? "default";

        TextureLoadingService.FetchTextureOrDefault(_skin.GetElementFilepathWithoutExtension("hitcircle"), "png", _use2x);
        TextureLoadingService.FetchTextureOrDefault(_skin.GetElementFilepathWithoutExtension($"{_hitcirclePrefix}-1"), "png", _use2x);
        TextureLoadingService.FetchTextureOrDefault(_skin.GetElementFilepathWithoutExtension("hitcircleoverlay"), "png", _use2x);
    }
}
