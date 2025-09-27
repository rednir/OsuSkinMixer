using System;
using System.IO;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Models.Realm;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Models.Osu;

public class OsuSkinLazer : OsuSkinBase
{
    public OsuSkinLazer(RealmOsuSkin realmSkin)
    {
        Name = realmSkin.Name;
        _files = realmSkin.Files.Select(f => new LazerFile(f.Filename, f.File.Hash)).ToList();
    }

    public override OsuSkinCredits Credits => throw new NotImplementedException();

    private IReadOnlyList<LazerFile> _files;

    public override OsuSkinFile TryGetFile(string virtualPath)
    {
        LazerFile fileUsage = _files.FirstOrDefault(f => f.Filename.Equals(virtualPath, StringComparison.OrdinalIgnoreCase));

        if (fileUsage is null)
            return null;

        string physicalPath = GetPhysicalPathFromHash(fileUsage.Hash);

        // TODO: should we check this?
        // if (!File.Exists(physicalPath))
        //     return null;

        return new OsuSkinFile(fileUsage.Filename, physicalPath);
    }

    protected override IEnumerable<OsuSkinFile> EnumerateFiles(SearchOption searchOption)
    {
        foreach (var file in _files)
        {
            string physicalPath = GetPhysicalPathFromHash(file.Hash);
            yield return new OsuSkinFile(file.Filename, physicalPath);
        }
    }

    private string GetPhysicalPathFromHash(string hash)
        => Path.Combine(Settings.Content.OsuFolder, "files", hash[..1], hash[..2], hash);
    
    private record LazerFile(string Filename, string Hash);
}
