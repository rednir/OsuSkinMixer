namespace OsuSkinMixer
{
    public class OptionInfo
    {
        public string Name { get; set; }

        public string NodePath => $"{Main.VBOX_CONTAINER_PATH}/{Name}/OptionButton";

        public SubOptionInfo[] SubOptions { get; set; }
    }
}