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
        _realmSkin = realmSkin;
    }

    public override string Name => _realmSkin.Name;

    public override OsuSkinCredits Credits => throw new NotImplementedException();

    private RealmOsuSkin _realmSkin;

    public override OsuSkinFile TryGetFile(string virtualPath)
    {
        RealmNamedFileUsage fileUsage = _realmSkin.Files.FirstOrDefault(f => f.Filename.Equals(virtualPath, StringComparison.OrdinalIgnoreCase));

        if (fileUsage is null)
            return null;

        string physicalPath = GetPhysicalPathFromHash(fileUsage.File.Hash);

        // TODO: should we check this?
        // if (!File.Exists(physicalPath))
        //     return null;

        return new OsuSkinFile(fileUsage.Filename, physicalPath);
    }

    protected override IEnumerable<OsuSkinFile> EnumerateFiles(SearchOption searchOption)
    {
        foreach (var fileUsage in _realmSkin.Files)
        {
            string physicalPath = GetPhysicalPathFromHash(fileUsage.File.Hash);
            yield return new OsuSkinFile(fileUsage.Filename, physicalPath);
        }
    }

    private string GetPhysicalPathFromHash(string hash)
        => Path.Combine(Settings.Content.OsuFolder, "files", hash[..1], hash[..2], hash);
}
