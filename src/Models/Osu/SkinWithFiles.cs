using System.IO;

namespace OsuSkinMixer.Models.Osu;

public class SkinWithFiles : Skin
{
    public SkinWithFiles(Skin skin)
    {
        Name = skin.Name;
        Directory = skin.Directory;
        SkinIni = skin.SkinIni;
        Files = skin.Directory.GetFiles();
    }

    public FileInfo[] Files { get; set; }
}