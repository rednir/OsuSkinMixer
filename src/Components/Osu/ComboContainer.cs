using Godot;
using OsuSkinMixer.Models;
using System;

namespace OsuSkinMixer.Components;

public partial class ComboContainer : HBoxContainer
{
    public OsuSkin Skin
    {
        get => _skin;
        set
        {
            _skin = value;
            if (ScoreX != null)
                ScoreX.Texture = value.Get2XTexture("score-x");
        }
    }

    private OsuSkin _skin;

    private int _combo;

    private TextureRect Tens;
    private TextureRect Ones;
    private TextureRect ScoreX;
    private AnimationPlayer AnimationPlayer;

    public override void _Ready()
    {
        Tens = GetNode<TextureRect>("%Tens");
        Ones = GetNode<TextureRect>("%Ones");
        ScoreX = GetNode<TextureRect>("%ScoreX");
        AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
    }

    public void Increment()
    {
        _combo++;

        int tens = _combo / 10;
        int ones = _combo % 10;

        if (tens > 0)
        {
            Tens.Texture = Skin.Get2XTexture($"score-{tens}");
            Tens.Visible = true;
        }

        Ones.Texture = Skin.Get2XTexture($"score-{ones}");
        AnimationPlayer.Play("increment");
    }

    public void Break()
    {
        if (_combo == 0)
            return;

        _combo = 0;

        Tens.Visible = false;
        Ones.Texture = Skin.Get2XTexture("score-0");
        AnimationPlayer.Play("break");
    }
}
