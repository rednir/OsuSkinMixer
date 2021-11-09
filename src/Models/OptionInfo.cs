namespace OsuSkinMixer
{
    public class OptionInfo
    {
        public string Name { get; set; }

        public string NodePath => $"{Main.SCROLL_CONTAINER_PATH}/{Name}/OptionButton";

        public SubOptionInfo[] SubOptions { get; set; }
    }
}