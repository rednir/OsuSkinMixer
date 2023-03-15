using Godot;
using System;
using OsuSkinMixer.Models;
using System.IO;
using OsuSkinMixer.Statics;
using SixLabors.ImageSharp;

namespace OsuSkinMixer.Components;

public partial class SkinPreview : PanelContainer
{
    private AnimationPlayer AnimationPlayer;
    private TextureRect MenuBackground;
    private Sprite2D Cursor;
    private AnimationPlayer CursorRotateAnimationPlayer;
    private Sprite2D Cursormiddle;
    private CpuParticles2D Cursortrail;
    private Hitcircle Hitcircle;

    private bool _paused;

    public override void _Ready()
    {
        AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
        MenuBackground = GetNode<TextureRect>("%MenuBackground");
        Cursor = GetNode<Sprite2D>("%Cursor");
        CursorRotateAnimationPlayer = GetNode<AnimationPlayer>("%CursorRotateAnimationPlayer");
        Cursormiddle = GetNode<Sprite2D>("%Cursormiddle");
        Cursortrail = GetNode<CpuParticles2D>("%Cursortrail");
        Hitcircle = GetNode<Hitcircle>("%Hitcircle");
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
        MenuBackground.Texture = skin.GetTexture("menu-background", "png", true) ?? skin.GetTexture("menu-background", "jpg");
        Cursor.Texture = skin.GetTexture("cursor");
        Cursormiddle.Texture = skin.GetTexture("cursormiddle");
        Cursortrail.Texture = skin.GetTexture("cursortrail");

        if (skin.SkinIni?.TryGetPropertyValue("General", "CursorRotate") is "1" or null)
            CursorRotateAnimationPlayer.Play("rotate");
        else
            CursorRotateAnimationPlayer.Stop();

        // TODO: this is very arbitrary, make this more accurate to osu!
        bool hasCursorMiddle = File.Exists($"{skin.Directory.FullName}/cursormiddle.png");
        bool transparentCursor = File.Exists($"{skin.Directory.FullName}/cursor.png")
            && Tools.GetContentRectFromImage($"{skin.Directory.FullName}/cursor.png") == Rectangle.Empty;

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

        Hitcircle.SetSkin(skin);
    }

    private void OnMouseEntered()
    {
        if (!_paused)
            return;

        _paused = false;
        Cursortrail.SpeedScale = 1;
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
        AnimationPlayer.Play("exit");
        Hitcircle.Pause();
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}
