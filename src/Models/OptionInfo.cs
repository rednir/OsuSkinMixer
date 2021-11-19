using System.Linq;

namespace OsuSkinMixer
{
    public class OptionInfo
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string NodePath => $"{Main.VBOX_CONTAINER_PATH}/{Name}/OptionButton";

        public SubOptionInfo[] SubOptions { get; set; }

        public override string ToString() => $"{Description} ({SubOptions.Length})";
    }
}