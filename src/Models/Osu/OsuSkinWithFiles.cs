using System.IO;

namespace OsuSkinMixer.Models;

public class OsuSkinWithFiles : OsuSkin
{
    public OsuSkinWithFiles(OsuSkin skin)
    {
        Name = skin.Name;
        Directory = skin.Directory;
        SkinIni = skin.SkinIni;
        Files = skin.Directory.GetFiles();
    }

    public FileInfo[] Files { get; set; }
}