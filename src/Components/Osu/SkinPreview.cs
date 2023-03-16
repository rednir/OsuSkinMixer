using Godot;
using System;
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
        Cursor = GetNode<Sprite2D>("%Cursor");
        CursorRotateAnimationPlayer = GetNode<AnimationPlayer>("%CursorRotateAnimationPlayer");
        Cursormiddle = GetNode<Sprite2D>("%Cursormiddle");
        Cursortrail = GetNode<CpuParticles2D>("%Cursortrail");
        Hitcircle = GetNode<Hitcircle>("%Hitcircle");

        VisibleOnScreenNotifier2D.ScreenEntered += OnScreenEntered;
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

        MenuBackground.Texture = _skin.GetTexture("menu-background", "png", true) ?? _skin.GetTexture("menu-background", "jpg");
        Cursor.Texture = _skin.GetTexture("cursor");
        Cursormiddle.Texture = _skin.GetTexture("cursormiddle");
        Cursortrail.Texture = _skin.GetTexture("cursortrail");

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
}
