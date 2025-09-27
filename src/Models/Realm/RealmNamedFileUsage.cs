using System;
using Realms;

namespace OsuSkinMixer.Models.Realm;

public class RealmNamedFileUsage : EmbeddedObject
{
    public string Filename { get; set; }

    public RealmFile File { get; set; }
}
