using System;
using System.IO;

namespace OsuSkinMixer.Models.Osu;

public class OsuSkinStable : OsuSkinBase
{
    public override OsuSkinCredits Credits
    {
        get
        {
            if (_credits is null)
                LoadCreditsFile($"{Directory.FullName}/{OsuSkinCredits.FILE_NAME}");

            return _credits;
        }
    }

    public DateTime LastWriteTime => Directory.LastWriteTime;

    public DirectoryInfo Directory { get; set; }

    public OsuSkinStable(string name, DirectoryInfo dir, bool hidden = false)
    {
        Name = name;
        Directory = dir;
        SkinIni = new OsuSkinIni(name, DEFAULT_AUTHOR);
        Hidden = hidden;
    }

    public OsuSkinStable(DirectoryInfo dir, bool hidden = false)
    {
        Name = dir.Name;
        Directory = dir;
        Hidden = hidden;

        // TODO: handle case sensitivity properly, although this covers most cases.
        LoadSkinIni(Path.Combine(dir.FullName, "skin.ini"), Path.Combine(dir.FullName, "Skin.ini"));
    }

    public override OsuSkinFile TryGetFile(string virtualPath)
    {
        foreach (OsuSkinFile file in Files)
        {
            if (string.Equals(file.VirtualPath, virtualPath, StringComparison.OrdinalIgnoreCase))
                return file;
        }

        return null;
    }

    protected override IEnumerable<OsuSkinFile> EnumerateFiles(SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        IEnumerable<FileInfo> files = Directory.EnumerateFiles("*", searchOption);

        foreach (FileInfo file in files)
        {
            string virtualPath = file.FullName[Directory.FullName.Length..]
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            yield return new OsuSkinFile(virtualPath, file.FullName);
        }
    }
}
