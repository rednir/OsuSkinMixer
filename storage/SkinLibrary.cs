using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace OsuSkinMixer.Storage;

public enum OsuClientKind { Stable, Lazer }

public sealed record SkinRecord(string Id, string Name, string Author, IReadOnlyDictionary<string, string> Files,
    string Revision, DateTime Modified, bool Hidden = false, bool IsLegacy = true, string? Problem = null);

public sealed record SkinSnapshot(SkinRecord Record, bool Deleted = false, string? ExpectedRevision = null);
public sealed record InstallResult(SkinRecord Skin, bool Reused = false, IReadOnlyList<SkinSnapshot>? Replaced = null);

/// <summary>Detached metadata only; Realm objects never escape this boundary.</summary>
public interface ISkinLibrary
{
    string Root { get; }
    OsuClientKind Kind { get; }
    string Status { get; }
    string? WriteRestriction { get; }
    IReadOnlyList<SkinRecord> Load();
    SkinWorkspace Materialize(SkinRecord skin, bool allowDegraded = false);
    InstallResult Install(SkinWorkspace workspace, bool overwriteExisting = false);
    void UndoInstall(InstallResult result);
    SkinRecord Replace(SkinRecord skin, SkinWorkspace workspace);
    SkinRecord Duplicate(SkinRecord skin, string name);
    InstallResult DuplicateWithResult(SkinRecord skin, string name, bool overwriteExisting = false);
    SkinRecord Rename(SkinRecord skin, string name);
    SkinSnapshot Delete(SkinRecord skin);
    SkinRecord Restore(SkinSnapshot snapshot);
    SkinRecord SetHidden(SkinRecord skin, bool hidden);
    void Export(SkinRecord skin, string path);
}

public static class SkinPaths
{
    public static string Canonical(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    public static string Identity(string path) => OperatingSystem.IsWindows() ? Canonical(path).ToUpperInvariant() : Canonical(path);

    public static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.IndexOfAny("<>:\"/\\|?*".ToCharArray()) >= 0 ||
            name.Any(char.IsControl) || name.EndsWith('.') || name.EndsWith(' ') || IsReserved(name))
            throw new InvalidDataException("Invalid skin name.");
        return name;
    }

    public static string Virtual(string name)
    {
        name = name.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(name) || name.StartsWith('/') || name.Split('/').Any(p =>
                p is "" or "." or ".." || p.IndexOfAny("<>:\"|?*".ToCharArray()) >= 0 || p.Any(char.IsControl) || p.EndsWith('.') || p.EndsWith(' ') || IsReserved(p)))
            throw new InvalidDataException($"Unsafe skin filename: {name}");
        return name;
    }
    private static bool IsReserved(string name)
    {
        var stem = name.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL" ||
            (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3] is >= '1' and <= '9');
    }
    public static void RejectLinks(string root, string path)
        => RejectLinks(root, path, null);

    internal static void RejectLinks(string root, string path, ISet<string>? validatedPaths)
    {
        root = Canonical(root);
        path = Canonical(path);
        if (Path.GetRelativePath(root, path).StartsWith("..")) throw new InvalidDataException("Path is outside the library.");
        for (string? current = path; current != null; current = Path.GetDirectoryName(current))
        {
            var identity = Identity(current);
            if (validatedPaths?.Contains(identity) != true)
            {
                var exists = File.Exists(current) || Directory.Exists(current);
                if (exists)
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("Linked library paths are not supported.");
                    validatedPaths?.Add(identity);
                }
            }
            if (Identity(current) == Identity(root)) break;
        }
    }

    public static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    public static Dictionary<string, string> Enumerate(string directory)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Walk(directory);
        return files;
        void Walk(string current)
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Linked directories/files are not supported in skin workspaces.");
            foreach (var entry in Directory.EnumerateFileSystemEntries(current))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Linked directories/files are not supported in skin workspaces.");
                if ((attributes & FileAttributes.Directory) != 0) Walk(entry);
                else if (!files.TryAdd(Virtual(Path.GetRelativePath(directory, entry)), entry))
                    throw new InvalidDataException("Skin has ambiguous case-insensitive filenames.");
            }
        }
    }
}

public sealed class SkinWorkspace : IDisposable
{
    public static string SessionRoot { get; } = Path.Combine(Path.GetTempPath(), "osu-skin-mixer", "workspaces", Guid.NewGuid().ToString("N"));
    public string DirectoryPath { get; }
    public SkinWorkspace()
    {
        DirectoryPath = Path.Combine(SessionRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
    }
    public static SkinWorkspace Copy(IReadOnlyDictionary<string, string> files, bool allowMissing = false)
    {
        var workspace = new SkinWorkspace();
        try
        {
            foreach (var pair in files)
            {
                var path = Path.Combine(workspace.DirectoryPath, SkinPaths.Virtual(pair.Key));
                if (allowMissing && !File.Exists(pair.Value)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.Copy(pair.Value, path);
            }
            return workspace;
        }
        catch { workspace.Dispose(); throw; }
    }
    public IReadOnlyDictionary<string, string> Files => SkinPaths.Enumerate(DirectoryPath);
    public void SetName(string name)
    {
        SkinPaths.ValidateName(name);
        var files = Files;
        var path = files.TryGetValue("skin.ini", out var ini) ? ini : Path.Combine(DirectoryPath, "skin.ini");
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();

        int firstGeneralSection = -1;
        int firstName = -1;
        bool inGeneralSection = false;
        for (int i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith('['))
            {
                inGeneralSection = trimmed.Equals("[General]", StringComparison.OrdinalIgnoreCase);
                if (inGeneralSection && firstGeneralSection < 0) firstGeneralSection = i;
                continue;
            }

            if (!inGeneralSection || !lines[i].Contains(':') ||
                !lines[i].Split(':', 2)[0].Trim().Equals("Name", StringComparison.OrdinalIgnoreCase)) continue;

            if (firstName < 0)
            {
                firstName = i;
                lines[i] = $"Name: {name}";
            }
            else
            {
                // Some legacy skins contain repeated General sections or duplicate Name
                // properties. Remove later values so metadata cannot override the new name.
                lines.RemoveAt(i--);
            }
        }

        if (firstGeneralSection < 0) lines.InsertRange(0, new[] { "[General]", $"Name: {name}" });
        else if (firstName < 0) lines.Insert(firstGeneralSection + 1, $"Name: {name}");

        File.WriteAllLines(path, lines);
    }
    public (string Name, string Author) Metadata()
    {
        var name = "Unnamed skin"; var author = "Unknown"; bool general = false;
        if (Files.TryGetValue("skin.ini", out var ini))
            foreach (var line in File.ReadLines(ini))
            {
                if (line.TrimStart().StartsWith('[')) general = line.Trim().Equals("[General]", StringComparison.OrdinalIgnoreCase);
                if (!general || !line.Contains(':')) continue;
                var parts = line.Split(':', 2);
                if (parts[0].Trim().Equals("Name", StringComparison.OrdinalIgnoreCase)) name = parts[1].Trim();
                if (parts[0].Trim().Equals("Author", StringComparison.OrdinalIgnoreCase)) author = parts[1].Trim();
            }
        return (name, author);
    }
    public void Export(string path)
    {
        // Never overwrite an existing user archive.
        ZipFile.CreateFromDirectory(DirectoryPath, path, CompressionLevel.Optimal, false);
    }
    public void Dispose() { if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true); }
}

public abstract class SkinLibrary : ISkinLibrary
{
    public string Root { get; }
    protected SkinLibrary(string root) => Root = SkinPaths.Canonical(root);
    public abstract OsuClientKind Kind { get; }
    public abstract string Status { get; }
    public virtual string? WriteRestriction => null;
    public abstract IReadOnlyList<SkinRecord> Load();
    public virtual SkinWorkspace Materialize(SkinRecord skin, bool allowDegraded = false)
    {
        if (!allowDegraded && skin.Problem != null) throw new InvalidDataException(skin.Problem);
        return SkinWorkspace.Copy(skin.Files, allowDegraded);
    }
    protected static void RequireWritable(SkinRecord skin)
    {
        if (!skin.IsLegacy) throw new InvalidOperationException("Only legacy-format user skins can be changed. Export this skin to edit it externally.");
        if (skin.Problem != null) throw new InvalidDataException(skin.Problem);
    }
    public abstract InstallResult Install(SkinWorkspace workspace, bool overwriteExisting = false);
    public virtual void UndoInstall(InstallResult result)
    {
        if (result.Reused) return;
        if (result.Replaced == null || result.Replaced.Count == 0) { Delete(result.Skin); return; }
        if (!result.Replaced.Any(s => s.Record.Id == result.Skin.Id)) Delete(result.Skin);
        foreach (var snapshot in result.Replaced) Restore(snapshot);
    }
    public abstract SkinRecord Replace(SkinRecord skin, SkinWorkspace workspace);
    public virtual SkinRecord Duplicate(SkinRecord skin, string name) => DuplicateWithResult(skin, name).Skin;
    public virtual InstallResult DuplicateWithResult(SkinRecord skin, string name, bool overwriteExisting = false)
    {
        RequireWritable(skin);
        using var workspace = Materialize(skin);
        workspace.SetName(name);
        return Install(workspace, overwriteExisting);
    }
    public virtual SkinRecord Rename(SkinRecord skin, string name)
    {
        RequireWritable(skin);
        using var workspace = Materialize(skin);
        workspace.SetName(name);
        return Replace(skin, workspace);
    }
    public abstract SkinSnapshot Delete(SkinRecord skin);
    public abstract SkinRecord Restore(SkinSnapshot snapshot);
    public abstract SkinRecord SetHidden(SkinRecord skin, bool hidden);
    public void Export(SkinRecord skin, string path)
    {
        using var workspace = Materialize(skin);
        var temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, ".osm-export-" + Guid.NewGuid().ToString("N") + ".osk");
        try { workspace.Export(temporary); File.Move(temporary, path, overwrite: true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
