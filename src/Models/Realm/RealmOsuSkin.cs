using System;
using Realms;

namespace OsuSkinMixer.Models.Realm;

[MapTo("Skin")]
public class RealmOsuSkin : RealmObject
{
    [PrimaryKey]
    public Guid ID { get; set; }

    public string Name { get; set; }

    public string Creator { get; set; }

    public string InstantiationInfo { get; set; } = null!;

    public string Hash { get; set; } = string.Empty;

    public bool Protected { get; set; }

    public IList<RealmNamedFileUsage> Files { get; }
}
