using Godot;

namespace OsuSkinMixer
{
    public class CreatingScreen : Panel
    {
        private AnimationPlayer AnimationPlayer;

        private TextureRect HitCircle;
        private TextureRect HitCircleOverlay;
        private TextureRect Default1;
        private TextureRect ApproachCircle;

        private Texture DefaultHitCircle;
        private Texture DefaultHitCircleOverlay;
        private Texture DefaultDefault1;
        private Texture DefaultApproachCircle;

        public override void _Ready()
        {
            AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            HitCircle = GetNode<TextureRect>("Circle/HitCircle");
            HitCircleOverlay = GetNode<TextureRect>("Circle/HitCircleOverlay");
            Default1 = GetNode<TextureRect>("Circle/Default1");
            ApproachCircle = GetNode<TextureRect>("Circle/ApproachCircle");

            DefaultHitCircle = HitCircle.Texture;
            DefaultHitCircleOverlay = HitCircleOverlay.Texture;
            DefaultDefault1 = Default1.Texture;
            DefaultApproachCircle = ApproachCircle.Texture;
        }

        public void ShowForSkin(string hitcircleSkin, string hitcircleoverlaySkin, string default1Skin, string approachcircleSkin)
        {
            var hitcircleImage = new Image();
            var hitcircleoverlayImage = new Image();
            var default1Image = new Image();
            var approachcircleImage = new Image();

            var hitcircleTexture = new ImageTexture();
            var hitcircleoverlayTexture = new ImageTexture();
            var default1Texture = new ImageTexture();
            var approachcircleTexture = new ImageTexture();

            string skinsFolder = Settings.Content.SkinsFolder;
            Error useHitcircle = hitcircleImage.Load($"{skinsFolder}/{hitcircleSkin}/hitcircle.png");
            Error useHitcircleoverlay = hitcircleoverlayImage.Load($"{skinsFolder}/{hitcircleoverlaySkin}/hitcircleoverlay.png");
            Error useDefault1 = default1Image.Load($"{skinsFolder}/{default1Skin}/default1.png");
            Error useApproachcircle = approachcircleImage.Load($"{skinsFolder}/{approachcircleSkin}/approachcircle.png");

            if (useHitcircle == Error.Ok)
                hitcircleTexture.CreateFromImage(hitcircleImage);
            if (useHitcircleoverlay == Error.Ok)
                hitcircleoverlayTexture.CreateFromImage(hitcircleoverlayImage);
            if (useDefault1 == Error.Ok)
                default1Texture.CreateFromImage(default1Image);
            if (useApproachcircle == Error.Ok)
                approachcircleTexture.CreateFromImage(approachcircleImage);

            HitCircle.Texture = useHitcircle == Error.Ok ? hitcircleTexture : DefaultHitCircle;
            HitCircleOverlay.Texture = useHitcircleoverlay == Error.Ok ? hitcircleoverlayTexture : DefaultHitCircleOverlay;
            Default1.Texture = useDefault1 == Error.Ok ? default1Texture : DefaultDefault1;
            ApproachCircle.Texture = useApproachcircle == Error.Ok ? approachcircleTexture : DefaultApproachCircle;

            AnimationPlayer.Play("in");
        }

        public void Finish()
        {
            AnimationPlayer.Play("out");
        }
    }
}