using System;
using Realms;

namespace OsuSkinMixer.Models.Realm;

[MapTo("File")]
public class RealmFile : RealmObject
{
    [PrimaryKey]
    public string Hash { get; set; }
}
