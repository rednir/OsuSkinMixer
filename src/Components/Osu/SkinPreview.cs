namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using System.IO;
using OsuSkinMixer.Statics;
using SixLabors.ImageSharp;
using OsuSkinMixer.Autoload;

public partial class SkinPreview : PanelContainer
{
    private TextureLoadingService TextureLoadingService;
    private VisibleOnScreenNotifier2D VisibleOnScreenNotifier2D;
    private AnimationPlayer AnimationPlayer;
    private TextureRect MenuBackground;
    private ComboContainer ComboContainer;
    private Sprite2D Cursor;
    private AnimationPlayer CursorRotateAnimationPlayer;
    private Sprite2D Cursormiddle;
    private CpuParticles2D Cursortrail;
    private Hitcircle Hitcircle;
    private AnimationPlayer LoadingPanelAnimationPlayer;

    private bool _paused;

    private OsuSkin _skin;

    private bool _isLoadTexturesCalled;

    private bool _isLoadFinished;

    public override void _Ready()
    {
        TextureLoadingService = GetNode<TextureLoadingService>("/root/TextureLoadingService");
        VisibleOnScreenNotifier2D = GetNode<VisibleOnScreenNotifier2D>("%VisibleOnScreenNotifier2D");
        AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
        MenuBackground = GetNode<TextureRect>("%MenuBackground");
        ComboContainer = GetNode<ComboContainer>("%ComboContainer");
        Cursor = GetNode<Sprite2D>("%Cursor");
        CursorRotateAnimationPlayer = GetNode<AnimationPlayer>("%CursorRotateAnimationPlayer");
        Cursormiddle = GetNode<Sprite2D>("%Cursormiddle");
        Cursortrail = GetNode<CpuParticles2D>("%Cursortrail");
        Hitcircle = GetNode<Hitcircle>("%Hitcircle");
        LoadingPanelAnimationPlayer = GetNode<AnimationPlayer>("%LoadingPanelAnimationPlayer");

        VisibleOnScreenNotifier2D.ScreenEntered += OnScreenEntered;
        VisibleOnScreenNotifier2D.ScreenExited += OnMouseExited;
        Hitcircle.CircleHit += OnCircleHit;

        TextureLoadingService.TextureReady += OnTextureReady;

        // Just in case?
        TreeExited += () => Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Process(double delta)
    {
        if (!_isLoadFinished)
            return;

        Vector2 mousePosition = GetGlobalMousePosition();
        Control hoveredControl = GetViewport().GuiGetHoveredControl();

        // The MouseEntered and MouseExited control node don't work as the hitcircle handles mouse input.
        if (GetGlobalRect().HasPoint(mousePosition) && hoveredControl is not null && IsAncestorOf(hoveredControl))
            OnMouseEntered();
        else
            OnMouseExited();

        if (_paused)
            return;

        Cursor.GlobalPosition = mousePosition;
        Cursormiddle.GlobalPosition = mousePosition;
    }

    public void SetSkin(OsuSkin skin)
    {
        _skin = skin;

        if (_isLoadTexturesCalled)
            LoadTextures();
    }

    private void OnTextureReady(string filepath, Texture2D texture, bool is2x)
    {
        if (!IsInstanceValid(this))
            return;

        if (filepath == _skin.GetElementFilepathWithoutExtension("menu-background"))
        {
            MenuBackground.SetDeferred(TextureRect.PropertyName.Texture, texture);

            // This is the last thing we called the load method for, and since all the textures 
            // we load for SkinPreview are from the same skin, they'll arrive in order.
            LoadingPanelAnimationPlayer.Play("out");
            _isLoadFinished = true;
        }
        else if (filepath == _skin.GetElementFilepathWithoutExtension("cursor"))
        {
            Cursor.SetDeferred(Sprite2D.PropertyName.Texture, texture);
            Cursor.SetDeferred(Sprite2D.PropertyName.Scale, is2x ? new Vector2(0.5f, 0.5f) : new Vector2(1, 1));
        }
        else if (filepath == _skin.GetElementFilepathWithoutExtension("cursormiddle"))
        {
            Cursormiddle.SetDeferred(TextureRect.PropertyName.Texture, texture);
            Cursormiddle.SetDeferred(TextureRect.PropertyName.Scale, is2x ? new Vector2(0.5f, 0.5f) : new Vector2(1, 1));
        }
        else if (filepath == _skin.GetElementFilepathWithoutExtension("cursortrail"))
        {
            Cursortrail.SetDeferred(CpuParticles2D.PropertyName.Texture, texture);
            Cursortrail.SetDeferred(CpuParticles2D.PropertyName.Scale, is2x ? new Vector2(0.5f, 0.5f) : new Vector2(1, 1));
        }
    }

    private void LoadTextures()
    {
        _isLoadTexturesCalled = true;
        LoadingPanelAnimationPlayer.Play("load");

        Hitcircle.SetSkin(_skin);
        ComboContainer.Skin = _skin;

        bool menuBgIsPng = File.Exists($"{_skin.Directory.FullName}/menu-background.png") || File.Exists($"{_skin.Directory.FullName}/menu-background@2x.png");

        TextureLoadingService.FetchTextureOrDefault(_skin.GetElementFilepathWithoutExtension("menu-background"), menuBgIsPng ? "png" : "jpg", true, 960);
        TextureLoadingService.FetchTextureOrDefault(_skin.GetElementFilepathWithoutExtension("cursor"));
        TextureLoadingService.FetchTextureOrDefault(_skin.GetElementFilepathWithoutExtension("cursormiddle"));
        TextureLoadingService.FetchTextureOrDefault(_skin.GetElementFilepathWithoutExtension("cursortrail"));

        if (_skin.SkinIni?.TryGetPropertyValue("General", "CursorRotate") is "1" or null)
            CursorRotateAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "rotate");
        else
            CursorRotateAnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Stop);

        bool hasCursorMiddle = File.Exists($"{_skin.Directory.FullName}/cursormiddle.png");
        bool transparentCursor = File.Exists($"{_skin.Directory.FullName}/cursor.png")
            && Tools.GetContentRectFromImage($"{_skin.Directory.FullName}/cursor.png") == Rectangle.Empty;

        // TODO: this is very arbitrary, make this more accurate to osu!
        if (hasCursorMiddle && transparentCursor)
        {
            Cursortrail.SetDeferred(CpuParticles2D.PropertyName.Lifetime, 0.5f);
            Cursortrail.SetDeferred(CpuParticles2D.PropertyName.Amount, 800);
        }
        else
        {
            Cursortrail.SetDeferred(CpuParticles2D.PropertyName.Lifetime, 0.15f);
            Cursortrail.SetDeferred(CpuParticles2D.PropertyName.Amount, 5);
        }

        // This seems to be how it works in osu! but idk really.
        Cursormiddle.SetDeferred(Sprite2D.PropertyName.Visible, hasCursorMiddle || !File.Exists($"{_skin.Directory.FullName}/cursor.png"));
    }

    private void OnScreenEntered()
    {
        if (_skin is null || _isLoadTexturesCalled)
            return;

        LoadTextures();
    }

    private void OnMouseEntered()
    {
        if (!_paused || !VisibleOnScreenNotifier2D.IsOnScreen())
        {
            // Hotfix for when the mouse is already in the preview window when the skin preview is created.
            if (!_paused && Input.MouseMode != Input.MouseModeEnum.Hidden)
                Input.MouseMode = Input.MouseModeEnum.Hidden;

            return;
        }

        _paused = false;
        Cursortrail.SpeedScale = 1;
        Cursortrail.Emitting = true;
        AnimationPlayer.Play("enter");
        Hitcircle.Resume();

        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    private void OnMouseExited()
    {
        if (_paused)
            return;

        _paused = true;
        Cursortrail.SpeedScale = 0;
        Cursortrail.Emitting = false;
        AnimationPlayer.Play("exit");
        Hitcircle.Pause();

        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void OnCircleHit(string score)
    {
        if (score == "0")
            ComboContainer.Break();
        else
            ComboContainer.Increment();
    }
}
