using Godot;

namespace OsuSkinMixer
{
    public class Background : Control
    {
        public override void _Ready()
        {
            Visible = !Settings.Content.DisableAnimatedBackground;
        }

        public override void _Process(float delta)
        {
            if (!Visible)
                return;

            float value = GetViewportRect().Size.x / 700;
            RectScale = new Vector2(value, value);
        }
    }
}