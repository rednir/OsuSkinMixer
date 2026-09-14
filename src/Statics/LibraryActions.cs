namespace OsuSkinMixer.Statics;

using System.IO;
using OsuSkinMixer.Models;
using OsuSkinMixer.Storage;

/// <summary>UI decisions marshalled to the main thread; storage code stays independent of Godot.</summary>
public static class LibraryActions
{
    public static string ExportPath(string name)
    {
        var folder = OsuData.Library?.Kind == OsuClientKind.Stable
            ? Path.Combine(OsuData.Library.Root, "Exports")
            : Path.Combine(Settings.AppdataFolderPath, "exports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var safeName = string.Concat(name.Select(c => "<>:\"/\\|?*".Contains(c) || char.IsControl(c) ? '_' : c));
        return Path.Combine(folder, $"{safeName}.osk");
    }
    public static void ExportAndOpen(OsuSkin skin)
    {
        var path = ExportPath(skin.Name);
        skin.Export(path);
        OpenArchive(path);
    }
    public static void OpenArchive(string path)
    {
        // OS file association handles macOS, Linux and Windows. Keep the archive even if launch fails.
        var result = OS.ShellOpen(path);
        Settings.PushToast(result == 0 ? "Opened the .osk with its associated application. Import is not confirmed yet. Archive retained in app exports." : "Could not open .osk automatically. Import the saved archive manually in osu!.");
        if (result != 0) OS.ShellOpen(Path.GetDirectoryName(path));
        OsuData.RequestRefresh();
    }
    public static SkinSnapshot Snapshot(OsuSkin skin)
    {
        if (skin.IsLazer) return new SkinSnapshot(skin.Record);
        // History owns this temporary copy until the application exits.
        var workspace = skin.CreateWorkspace();
        return new SkinSnapshot(skin.Record with { Files = workspace.Files });
    }

    public static void TrackImport(OsuSkin generated)
    {
        var library = OsuData.Library;
        using var workspace = generated.CreateWorkspace();
        var expected = workspace.Files.ToDictionary(p => p.Key, p => SkinPaths.Hash(p.Value), StringComparer.OrdinalIgnoreCase);
        _ = Task.Run(async () =>
        {
            for (int attempt = 0; attempt < 60 && OsuData.Library == library; attempt++)
            {
                await Task.Delay(1500);
                try
                {
                    var match = library.Load().FirstOrDefault(s => s.Files.Count == expected.Count && s.Files.All(f =>
                        expected.TryGetValue(f.Key, out var hash) && Path.GetFileName(f.Value) == hash));
                    if (match == null) continue;
                    OsuData.RequestRefresh();
                    Settings.PushToast($"Confirmed skin in the selected lazer library: {match.Name}");
                    return;
                }
                catch (Exception e) { Settings.Log($"Waiting for lazer import: {e.Message}"); }
            }
            Settings.PushToast("Import was not confirmed in the selected library. Your .osk is retained in app exports; you can import it manually.");
        });
    }
}
