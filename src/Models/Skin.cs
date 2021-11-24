using System.IO;
using System.Linq;

namespace OsuSkinMixer
{
    public class Skin
    {
        public Skin(DirectoryInfo dir)
        {
            Name = dir.Name;
            Directory = dir;
            if (File.Exists($"{dir.FullName}/skin.ini"))
                SkinIni = new SkinIni(File.ReadAllText($"{dir.FullName}/skin.ini"));
        }

        public string Name { get; set; }

        public DirectoryInfo Directory { get; set; }

        public SkinIni SkinIni { get; set; }
    }
}