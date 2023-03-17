using System;

namespace OsuSkinMixer.Models;

[Flags]
public enum ManageSkinOptions
{
    None = 0,
    OpenInOsu = 1,
    OpenFolder = 2,
    Modify = 4,
    Hide = 8,
    Export = 16,
    Duplicate = 32,
    Delete = 64,
    All = OpenInOsu | OpenFolder | Modify | Hide | Export | Duplicate | Delete,
}