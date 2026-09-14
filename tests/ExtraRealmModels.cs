using Realms;

namespace OsuSkinMixer.Tests;

public class UnrelatedData : RealmObject
{
    [PrimaryKey] public string ID { get; set; } = "";
    public string Value { get; set; } = "";
}

[MapTo("Skin")]
public class IncompatibleSkin : RealmObject
{
    [PrimaryKey] public string ID { get; set; } = "";
}

[MapTo("Skin")]
public class ExtendedSkin : RealmObject
{
    [PrimaryKey] public Guid ID { get; set; }
    public string Name { get; set; } = "";
    public string Creator { get; set; } = "";
    public string InstantiationInfo { get; set; } = "";
    public string Hash { get; set; } = "";
    public bool Protected { get; set; }
    public bool DeletePending { get; set; }
    public IList<OsuSkinMixer.Storage.RealmNamedFileUsage> Files { get; } = null!;
    public string FutureProperty { get; set; } = "";
}
