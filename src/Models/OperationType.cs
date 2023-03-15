using System;
using System.Text.Json.Serialization;

namespace OsuSkinMixer.Models;

public enum OperationType
{
    SkinMixer,
    SkinModifier,
    Delete,
    Duplicate,
    Hide,
    Unhide,
    Export,
    Other,
}