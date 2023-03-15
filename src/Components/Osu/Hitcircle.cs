using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class Hitcircle : Node2D
{
    const int OD = 6;

    const double HIT_300_TIME_MSEC = 1000;

    private AnimationPlayer CircleAnimationPlayer;
    private AnimationPlayer HitJudgementAnimationPlayer;
    private AnimatedSprite2D HitJudgementSprite;
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
        HitJudgementAnimationPlayer = GetNode<AnimationPlayer>("%HitJudgementAnimationPlayer");
        HitJudgementSprite = GetNode<AnimatedSprite2D>("%HitJudgementSprite");
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

        HitJudgementSprite.SpriteFrames = skin.GetSpriteFrames("hit0", "hit50", "hit100", "hit300");
        ApproachcircleSprite.Texture = skin.GetTexture("approachcircle");
        HitcircleSprite.Texture = skin.GetTexture("hitcircle");
        HitcircleoverlaySprite.Texture = skin.GetTexture("hitcircleoverlay");

        _comboColors = skin.ComboColors;
        NextCombo();
    }

    public void Pause()
    {
        CircleAnimationPlayer.Pause();
        HitJudgementAnimationPlayer.Pause();
        HitJudgementSprite.Pause();
    }

    public void Resume()
    {
        if (CircleAnimationPlayer.AssignedAnimation != string.Empty)
            CircleAnimationPlayer.Play();

        if (HitJudgementAnimationPlayer.AssignedAnimation != string.Empty)
            HitJudgementAnimationPlayer.Play();

        if (HitJudgementSprite.FrameProgress > 0.0 && HitJudgementSprite.FrameProgress < 1.0)
            HitJudgementSprite.Play();
    }

    private void NextCombo()
    {
        _currentComboIndex = (_currentComboIndex + 1) % _comboColors.Length;

        HitcircleSprite.Modulate = _comboColors[_currentComboIndex];
        ApproachcircleSprite.SelfModulate = _comboColors[_currentComboIndex];
        DefaultSprite.Texture = _skin.GetTexture($"default-{(_currentComboIndex == 0 ? _comboColors.Length : _currentComboIndex)}");
    }

    private void Hit(string score)
    {
        CircleAnimationPlayer.Play(score == "0" ? "miss" : "hit");
        HitJudgementSprite.Play($"hit{score}");
        HitJudgementAnimationPlayer.Play("show");
    }

    private void OnInputEvent(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            double animationPosition = CircleAnimationPlayer.CurrentAnimationPosition * 1000;
            switch (Math.Abs(HIT_300_TIME_MSEC - animationPosition))
            {
                case < 80 - (6 * OD):
                    Hit("300");
                    break;
                case < 140 - (10 * OD):
                    Hit("100");
                    break;
                case < 200 - (10 * OD):
                    Hit("50");
                    break;
                default:
                    Hit("0");
                    break;
            }
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
            Hit("0");
    }
}
