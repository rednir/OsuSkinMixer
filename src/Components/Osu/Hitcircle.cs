namespace OsuSkinMixer.Components;

using System.IO;
using OsuSkinMixer.Autoload;
using OsuSkinMixer.Models;

public partial class Hitcircle : Node2D
{
    const int OD = 4;

    const double HIT_300_TIME_MSEC = 1000;

    [Signal]
    public delegate void CircleHitEventHandler(string score);

    private TextureLoadingService TextureLoadingService;
    private AudioStreamPlayer HitSoundPlayer;
    private AudioStreamPlayer ComboBreakPlayer;
    private AnimationPlayer CircleAnimationPlayer;
    private AnimationPlayer HitJudgementAnimationPlayer;
    private AnimatedSprite2D HitJudgementSprite;
    private Sprite2D ApproachcircleSprite;
    private Sprite2D HitcircleSprite;
    private Sprite2D HitcircleoverlaySprite;
    private Sprite2D DefaultSprite;
    private Control Control;

    private OsuSkin _skin;

    private string _hitcirclePrefix;

    private Color[] _comboColors;

    private int _currentComboIndex;

    private int _combo;

    private bool _use2x;

    public override void _Ready()
    {
        TextureLoadingService = GetNode<TextureLoadingService>("/root/TextureLoadingService");
        HitSoundPlayer = GetNode<AudioStreamPlayer>("%HitSoundPlayer");
        ComboBreakPlayer = GetNode<AudioStreamPlayer>("%ComboBreakPlayer");
        CircleAnimationPlayer = GetNode<AnimationPlayer>("%CircleAnimationPlayer");
        HitJudgementAnimationPlayer = GetNode<AnimationPlayer>("%HitJudgementAnimationPlayer");
        HitJudgementSprite = GetNode<AnimatedSprite2D>("%HitJudgementSprite");
        ApproachcircleSprite = GetNode<Sprite2D>("%ApproachcircleSprite");
        HitcircleSprite = GetNode<Sprite2D>("%HitcircleSprite");
        HitcircleoverlaySprite = GetNode<Sprite2D>("%HitcircleoverlaySprite");
        DefaultSprite = GetNode<Sprite2D>("%DefaultSprite");
        Control = GetNode<Control>("%Control");

        TextureLoadingService.TextureReady += OnTextureReady;
        CircleAnimationPlayer.AnimationFinished += OnAnimationFinished;
        Control.GuiInput += OnInputEvent;
    }

    public void SetSkin(OsuSkin skin)
    {
        _skin = skin;

        HitSoundPlayer.SetDeferred(AudioStreamPlayer.PropertyName.Stream, skin.GetAudioStream("normal-hitnormal"));
        ComboBreakPlayer.SetDeferred(AudioStreamPlayer.PropertyName.Stream, skin.GetAudioStream("combobreak"));

        // We'll add animations as we need them.
        HitJudgementSprite.SpriteFrames = new SpriteFrames();

        _use2x = File.Exists($"{_skin.Directory}/approachcircle@2x.png")
            && File.Exists($"{_skin.Directory}/hitcircle@2x.png")
            && File.Exists($"{_skin.Directory}/hitcircleoverlay@2x.png");

        TextureLoadingService.FetchTextureOrDefault(skin.GetElementFilepathWithoutExtension("approachcircle"), "png", _use2x);
        TextureLoadingService.FetchTextureOrDefault(skin.GetElementFilepathWithoutExtension("hitcircle"), "png", _use2x);
        TextureLoadingService.FetchTextureOrDefault(skin.GetElementFilepathWithoutExtension("hitcircleoverlay"), "png", _use2x);

        SetDeferred(Node2D.PropertyName.Scale, _use2x ? new Vector2(0.5f, 0.5f) : new Vector2(1, 1));

        _comboColors = skin.ComboColors;
        _hitcirclePrefix = skin.SkinIni.TryGetPropertyValue("Fonts", "HitCirclePrefix") ?? "default";

        // Prevent scale being read before it is set.
        CallDeferred(nameof(NextCombo));
    }

    private void OnTextureReady(string filepath, Texture2D texture, bool is2x)
    {
        if (!IsInstanceValid(this) || _skin is null)
            return;

        if (filepath == _skin.GetElementFilepathWithoutExtension("approachcircle"))
        {
            ApproachcircleSprite.SetDeferred(Sprite2D.PropertyName.Texture, texture);
        }
        else if (filepath == _skin.GetElementFilepathWithoutExtension("hitcircle"))
        {
            HitcircleSprite.SetDeferred(Sprite2D.PropertyName.Texture, texture);
        }
        else if (filepath == _skin.GetElementFilepathWithoutExtension("hitcircleoverlay"))
        {
            HitcircleoverlaySprite.SetDeferred(Sprite2D.PropertyName.Texture, texture);
        }
        else if (filepath.StartsWith(_skin.GetElementFilepathWithoutExtension(_hitcirclePrefix) + "-"))
        {
            DefaultSprite.SetDeferred(Sprite2D.PropertyName.Texture, texture);
        }
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

        HitcircleSprite.SetDeferred(Sprite2D.PropertyName.Modulate, _comboColors[_currentComboIndex]);
        ApproachcircleSprite.SetDeferred(Sprite2D.PropertyName.SelfModulate, _comboColors[_currentComboIndex]);

        // Hitcircle scale is set previously based on whether the textures are @2x or not.
        string filename = $"{_hitcirclePrefix}-{(_currentComboIndex == 0 ? _comboColors.Length : _currentComboIndex)}";
        TextureLoadingService.FetchTextureOrDefault(_skin.GetElementFilepathWithoutExtension(filename), "png", _use2x);
    }

    private void Hit(string score)
    {
        if (score == "0")
        {
            CircleAnimationPlayer.Play("miss");
            if (_combo > 0)
            {
                _combo = 0;
                ComboBreakPlayer.Play();
            }
        }
        else
        {
            _combo++;
            HitSoundPlayer.Play();
            CircleAnimationPlayer.Play("hit");
        }

        string hitJudgementAnimationName = $"hit{score}";

        if (!HitJudgementSprite.SpriteFrames.HasAnimation(hitJudgementAnimationName))
            _skin.AddSpriteFramesAnimation(HitJudgementSprite.SpriteFrames, hitJudgementAnimationName, _use2x);

        // Only play the falling effect is there is not a miss animation in the skin.
        HitJudgementAnimationPlayer.Play(
            score == "0" && HitJudgementSprite.SpriteFrames.GetFrameCount(hitJudgementAnimationName) <= 1 ? "show_miss" : "show");

        HitJudgementSprite.Play(hitJudgementAnimationName);
        EmitSignal(SignalName.CircleHit, score);
    }

    private void OnInputEvent(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex is MouseButton.Left or MouseButton.Right)
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
