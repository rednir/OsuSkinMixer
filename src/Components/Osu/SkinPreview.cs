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

    private bool _paused;

    private OsuSkin _skin;

    private bool _isTexturesLoaded;

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

        VisibleOnScreenNotifier2D.ScreenEntered += OnScreenEntered;
        VisibleOnScreenNotifier2D.ScreenExited += OnMouseExited;
        Hitcircle.CircleHit += OnCircleHit;

        // Just in case?
        TreeExited += () => Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Process(double delta)
    {
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

        TextureLoadingService.TextureReady += OnTextureReady;

        if (_isTexturesLoaded)
            LoadTextures();
    }

    private void OnTextureReady(string filepath, Texture2D texture)
    {
        if (!IsInstanceValid(this))
            return;

        if (filepath == _skin.GetElementFilepath("menu-background", "jpg"))
        {
            MenuBackground.SetDeferred(TextureRect.PropertyName.Texture, texture);
        }
    }

    private void LoadTextures()
    {
        _isTexturesLoaded = true;

        TextureLoadingService.GetTextureInBackground(_skin.GetElementFilepath("menu-background", "jpg"), 960);

        // Scale textures based on whether they are @2x or not.
        float cursorTrailScale = _skin.TryGet2XTexture("cursortrail", out var cursortrail) ? 0.5f : 1;
        Cursor.SetDeferred(Sprite2D.PropertyName.Scale, _skin.TryGet2XTexture("cursor", out var cursor) ? new Vector2(0.5f, 0.5f) : new Vector2(1, 1));
        Cursormiddle.SetDeferred(Sprite2D.PropertyName.Scale, _skin.TryGet2XTexture("cursormiddle", out var cursormiddle) ? new Vector2(0.5f, 0.5f) : new Vector2(1, 1));
        Cursortrail.SetDeferred(CpuParticles2D.PropertyName.ScaleAmountMax, cursorTrailScale);
        Cursortrail.SetDeferred(CpuParticles2D.PropertyName.ScaleAmountMin, cursorTrailScale);

        Cursor.SetDeferred(TextureRect.PropertyName.Texture, cursor);
        Cursormiddle.SetDeferred(TextureRect.PropertyName.Texture, cursormiddle);
        Cursortrail.SetDeferred(CpuParticles2D.PropertyName.Texture, cursortrail);

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

        Hitcircle.SetSkin(_skin);
        ComboContainer.Skin = _skin;
    }

    private void OnScreenEntered()
    {
        if (_skin == null || _isTexturesLoaded)
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
