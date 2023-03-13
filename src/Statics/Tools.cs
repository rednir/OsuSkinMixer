using Godot;
using File = System.IO.File;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Statics;

public static class Tools
{
    public static void ShellOpenFile(string path)
        => OS.ShellOpen(OS.GetName() == "macOS" ? $"file://{path}" : path);

    public static void TriggerOskImport(OsuSkin skin)
    {
        string oskDestPath = $"{Settings.SkinsFolderPath}/{skin.Name}.osk";
        Settings.Log($"Importing skin into game from '{oskDestPath}'");

        // osu! will handle the empty .osk (zip) file by switching the current skin to the skin with name `newSkinName`.
        File.WriteAllBytes(oskDestPath, new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        ShellOpenFile(oskDestPath);
    }
}