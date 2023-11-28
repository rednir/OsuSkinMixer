namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using System.IO;

public partial class ComboContainer : HBoxContainer
{
    public OsuSkin Skin
    {
        get => _skin;
        set
        {
            _skin = value;

            string comboPrefix = value.SkinIni.TryGetPropertyValue("Fonts", "ComboPrefix") ?? "score";

            bool comboPrefixExists = File.Exists(Path.Combine(value.Directory.FullName, $"{comboPrefix}-x.png"))
                || File.Exists(Path.Combine(value.Directory.FullName, $"{comboPrefix}-x@2x.png"));

            _comboPrefix = comboPrefixExists ? comboPrefix : "score";

            ScoreX?.SetDeferred(TextureRect.PropertyName.Texture, value.Get2XTexture($"{_comboPrefix}-x"));
        }
    }

    private OsuSkin _skin;

    private string _comboPrefix;

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
            Tens.Texture = Skin.Get2XTexture($"{_comboPrefix}-{tens}");
            Tens.Visible = true;
        }

        Ones.Texture = Skin.Get2XTexture($"{_comboPrefix}-{ones}");
        AnimationPlayer.Play("increment");
    }

    public void Break()
    {
        if (_combo == 0)
            return;

        _combo = 0;

        Tens.Visible = false;
        Ones.Texture = Skin.Get2XTexture($"{_comboPrefix}-0");
        AnimationPlayer.Play("break");
    }
}
