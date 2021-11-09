using Godot;

namespace OsuSkinMixer
{
    public class Background : Control
    {
        public override void _Process(float delta)
        {
            float value = GetViewportRect().Size.x / 700;
            RectScale = new Vector2(value, value);
        }
    }
}