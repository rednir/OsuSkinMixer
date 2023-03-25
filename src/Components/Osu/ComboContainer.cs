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
				ScoreX.Texture = value.GetTexture("score-x");
        }
    }

    private int _combo;

    private TextureRect Tens;
    private TextureRect Ones;
    private TextureRect ScoreX;
    private OsuSkin _skin;

    public override void _Ready()
    {
        Tens = GetNode<TextureRect>("%Tens");
        Ones = GetNode<TextureRect>("%Ones");
        ScoreX = GetNode<TextureRect>("%ScoreX");
    }

    public void Increment()
    {
        _combo++;

        int tens = _combo / 10;
        int ones = _combo % 10;

        if (tens > 0)
        {
            Tens.Texture = Skin.GetTexture($"score-{tens}");
            Tens.Visible = true;
        }

        Ones.Texture = Skin.GetTexture($"score-{ones}");
    }

    public void Break()
    {
        _combo = 0;

        Tens.Visible = false;
        Ones.Texture = Skin.GetTexture("score-0");
    }
}
