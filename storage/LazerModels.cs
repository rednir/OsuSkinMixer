using Realms;

namespace OsuSkinMixer.Storage;

[MapTo("Skin")]
public class LazerSkin : RealmObject
{
    [PrimaryKey] public Guid ID { get; set; }
    public string Name { get; set; } = "";
    public string Creator { get; set; } = "";
    public string InstantiationInfo { get; set; } = "";
    public string Hash { get; set; } = "";
    public bool Protected { get; set; }
    public bool DeletePending { get; set; }
    public IList<RealmNamedFileUsage> Files { get; } = null!;
}

public class RealmNamedFileUsage : EmbeddedObject
{
    public string Filename { get; set; } = "";
    public LazerFile File { get; set; } = null!;
}

[MapTo("File")]
public class LazerFile : RealmObject
{
    [PrimaryKey] public string Hash { get; set; } = "";
}
