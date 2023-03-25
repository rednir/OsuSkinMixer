using Godot;
using OsuSkinMixer.Models;
using System.IO;
using OsuSkinMixer.Statics;
using SixLabors.ImageSharp;

namespace OsuSkinMixer.Components;

public partial class SkinPreview : PanelContainer
{
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
        Hitcircle.CircleHit += OnCircleHit;
    }

    public override void _Process(double delta)
    {
        Vector2 mousePosition = GetGlobalMousePosition();

        // The MouseEntered and MouseExited control node don't work as the hitcircle handles mouse input.
        if (GetGlobalRect().HasPoint(mousePosition))
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

        if (_isTexturesLoaded)
            LoadTextures();
    }

    private void LoadTextures()
    {
        _isTexturesLoaded = true;

        _skin.TryGetTexture("menu-background", out var menuBackground, "jpg");

        // Scale textures based on whether they are @2x or not.
        Cursor.Scale = _skin.TryGet2XTexture("cursor", out var cursor) ? new Vector2(0.5f, 0.5f) : new Vector2(1, 1);
        Cursormiddle.Scale = _skin.TryGet2XTexture("cursormiddle", out var cursormiddle) ? new Vector2(0.5f, 0.5f) : new Vector2(1, 1);
        Cursortrail.ScaleAmountMax = Cursortrail.ScaleAmountMin = _skin.TryGet2XTexture("cursortrail", out var cursortrail) ? 0.5f : 1;

        MenuBackground.Texture = menuBackground;
        Cursor.Texture = cursor;
        Cursormiddle.Texture = cursormiddle;
        Cursortrail.Texture = cursortrail;

        if (_skin.SkinIni?.TryGetPropertyValue("General", "CursorRotate") is "1" or null)
            CursorRotateAnimationPlayer.Play("rotate");
        else
            CursorRotateAnimationPlayer.Stop();

        bool hasCursorMiddle = File.Exists($"{_skin.Directory.FullName}/cursormiddle.png");
        bool transparentCursor = File.Exists($"{_skin.Directory.FullName}/cursor.png")
            && Tools.GetContentRectFromImage($"{_skin.Directory.FullName}/cursor.png") == Rectangle.Empty;

        // TODO: this is very arbitrary, make this more accurate to osu!
        if (hasCursorMiddle && transparentCursor)
        {
            Cursortrail.Lifetime = 0.5f;
            Cursortrail.Amount = 800;
        }
        else
        {
            Cursortrail.Lifetime = 0.15f;
            Cursortrail.Amount = 5;
        }

        // This seems to be how it works in osu! but idk really.
        Cursormiddle.Visible = hasCursorMiddle || !File.Exists($"{_skin.Directory.FullName}/cursor.png");

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
        if (!_paused)
            return;

        _paused = false;
        Cursortrail.SpeedScale = 1;
        Cursortrail.Emitting = true;
        AnimationPlayer.Play("enter");
        Hitcircle.Resume();
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
    }

    private void OnCircleHit(string score)
    {
        if (score == "0")
            ComboContainer.Break();
        else
            ComboContainer.Increment();
    }
}
