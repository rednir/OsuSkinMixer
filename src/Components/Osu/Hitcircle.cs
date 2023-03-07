using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class Hitcircle : Node2D
{
    private AnimationPlayer CircleAnimationPlayer;
    private Sprite2D ApproachcircleSprite;
    private Sprite2D HitcircleSprite;
    private Sprite2D HitcircleoverlaySprite;
    private Sprite2D DefaultSprite;
    private Control Control;

    private OsuSkin _skin;

    private Color[] _comboColors;

    private int _currentComboIndex;

    public override void _Ready()
    {
        CircleAnimationPlayer = GetNode<AnimationPlayer>("%CircleAnimationPlayer");
        ApproachcircleSprite = GetNode<Sprite2D>("%ApproachcircleSprite");
        HitcircleSprite = GetNode<Sprite2D>("%HitcircleSprite");
        HitcircleoverlaySprite = GetNode<Sprite2D>("%HitcircleoverlaySprite");
        DefaultSprite = GetNode<Sprite2D>("%DefaultSprite");
        Control = GetNode<Control>("%Control");

        CircleAnimationPlayer.AnimationFinished += OnAnimationFinished;
        Control.GuiInput += OnInputEvent;
    }

    public void SetSkin(OsuSkin skin)
    {
        _skin = skin;

        ApproachcircleSprite.Texture = skin.GetTexture("approachcircle.png");
        HitcircleSprite.Texture = skin.GetTexture("hitcircle.png");
        HitcircleoverlaySprite.Texture = skin.GetTexture("hitcircleoverlay.png");

        _comboColors = skin.ComboColors;
		NextCombo();
    }

    public void Pause()
    {
        CircleAnimationPlayer.Pause();
    }

    public void Resume()
    {
        CircleAnimationPlayer.Play();
    }

    private void NextCombo()
    {
        _currentComboIndex = (_currentComboIndex + 1) % _comboColors.Length;

        HitcircleSprite.Modulate = _comboColors[_currentComboIndex];
        DefaultSprite.Texture = _skin.GetTexture($"default-{(_currentComboIndex == 0 ? _comboColors.Length : _currentComboIndex)}.png");
    }

    private void OnInputEvent(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            CircleAnimationPlayer.Play("hit");
        }
    }

    private void OnAnimationFinished(StringName animationName)
    {
        if (animationName == "hit" || animationName == "miss")
        {
            NextCombo();
            CircleAnimationPlayer.Play("fade_in");
        }

        if (animationName == "fade_in")
            CircleAnimationPlayer.Play("miss");
    }
}
