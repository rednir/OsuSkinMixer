namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;
using System.IO;

public partial class ComboContainer : HBoxContainer
{
    public OsuSkinBase Skin
    {
        get => _skin;
        set
        {
            _skin = value;

            // TODO: lazer
            // string comboPrefix = value.SkinIni.TryGetPropertyValue("Fonts", "ComboPrefix") ?? "score";

            // bool comboPrefixExists = File.Exists(Path.Combine(value.Directory.FullName, $"{comboPrefix}-x.png"))
            //     || File.Exists(Path.Combine(value.Directory.FullName, $"{comboPrefix}-x@2x.png"));

            // _comboPrefix = comboPrefixExists ? comboPrefix : "score";

            // ScoreX?.SetDeferred(TextureRect.PropertyName.Texture, value.Get2XTexture($"{_comboPrefix}-x"));
        }
    }

    public int Combo { get; private set; }

    private OsuSkinBase _skin;

    private string _comboPrefix;

    private TextureRect Tens;
    private TextureRect Ones;
    private TextureRect ScoreX;
    private AnimationPlayer AnimationPlayer;
    private OkPopup OkPopup;

    public override void _Ready()
    {
        Tens = GetNode<TextureRect>("%Tens");
        Ones = GetNode<TextureRect>("%Ones");
        ScoreX = GetNode<TextureRect>("%ScoreX");
        AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
        OkPopup = GetNode<OkPopup>("%OkPopup");
    }

    public void Increment()
    {
        // Combo++;

        // int tens = Combo / 10;
        // int ones = Combo % 10;

        // if (tens > 0)
        // {
        //     Tens.Texture = Skin.Get2XTexture($"{_comboPrefix}-{tens}");
        //     Tens.Visible = true;
        // }

        // Ones.Texture = Skin.Get2XTexture($"{_comboPrefix}-{ones}");
        // AnimationPlayer.Play("increment");

        // if (Combo == 100)
        //     OkPopup.In();
    }

    public void Break()
    {
        // if (Combo == 0)
        //     return;

        // Combo = 0;

        // Tens.Visible = false;
        // Ones.Texture = Skin.Get2XTexture($"{_comboPrefix}-0");
        // AnimationPlayer.Play("break");
    }
}
