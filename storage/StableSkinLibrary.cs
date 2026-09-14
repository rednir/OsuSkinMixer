namespace OsuSkinMixer.Storage;

public sealed class StableSkinLibrary : SkinLibrary
{
    private readonly string recoveryRoot;
    public StableSkinLibrary(string root, string recoveryRoot) : base(root) => this.recoveryRoot = recoveryRoot;
    public override OsuClientKind Kind => OsuClientKind.Stable;
    public override string Status => "osu!stable · folder storage";
    public override IReadOnlyList<SkinRecord> Load()
    {
        var result = new List<SkinRecord>();
        foreach (var folder in new[] { "Skins", "HiddenSkins" })
        {
            var path = Path.Combine(Root, folder);
            if (!Directory.Exists(path)) continue;
            foreach (var directory in Directory.EnumerateDirectories(path).Where(p => !Path.GetFileName(p).StartsWith(".osm-")))
                result.Add(Read(directory, folder == "HiddenSkins"));
        }
        return result;
    }
    private static SkinRecord Read(string path, bool hidden = false)
    {
        var files = SkinPaths.Enumerate(path);
        var revision = string.Join('|', files.OrderBy(p => p.Key).Select(p => $"{p.Key}:{new FileInfo(p.Value).Length}:{File.GetLastWriteTimeUtc(p.Value).Ticks}"));
        string author = "Unknown";
        if (files.TryGetValue("skin.ini", out var ini))
            author = File.ReadLines(ini).FirstOrDefault(l => l.Split(':')[0].Trim().Equals("Author", StringComparison.OrdinalIgnoreCase))?.Split(':', 2).Last().Trim() ?? author;
        return new(SkinPaths.Identity(path), Path.GetFileName(path), author, files, revision, Directory.GetLastWriteTimeUtc(path), hidden);
    }
    private string Resolve(SkinRecord skin)
    {
        var path = SkinPaths.Canonical(skin.Id);
        SkinPaths.RejectLinks(Root, path);
        var parent = SkinPaths.Identity(Path.GetDirectoryName(path)!);
        if (parent != SkinPaths.Identity(Path.Combine(Root, "Skins")) && parent != SkinPaths.Identity(Path.Combine(Root, "HiddenSkins")))
            throw new InvalidOperationException("Skin does not belong to this library.");
        return path;
    }
    private void EnsureCurrent(SkinRecord skin)
    {
        if (Read(Resolve(skin), skin.Hidden).Revision != skin.Revision)
            throw new IOException("Skin changed externally. Refresh and try again.");
    }
    public override SkinRecord Duplicate(SkinRecord skin, string name)
    {
        EnsureCurrent(skin);
        using var workspace = Materialize(skin);
        workspace.SetName(name);
        return Install(workspace, overwriteExisting: true).Skin;
    }
    public override InstallResult Install(SkinWorkspace workspace, bool overwriteExisting = false)
    {
        var name = SkinPaths.ValidateName(workspace.Metadata().Name);
        var target = Path.Combine(Root, "Skins", name);
        var hiddenPath = Path.Combine(Root, "HiddenSkins", name);
        if (!overwriteExisting && (Directory.Exists(target) || Directory.Exists(hiddenPath)))
            throw new IOException("A skin with this name already exists. Choose another name or modify the existing skin.");
        var previous = new List<SkinSnapshot>();
        foreach (var path in new[] { target, hiddenPath }.Where(Directory.Exists))
        {
            var record = Read(path, path == hiddenPath);
            var recovery = Path.Combine(recoveryRoot, Guid.NewGuid().ToString("N"));
            CopyDirectory(path, recovery);
            previous.Add(new(record with { Files = SkinPaths.Enumerate(recovery) }, path == hiddenPath));
        }
        try
        {
            Commit(workspace, target);
            if (Directory.Exists(hiddenPath))
            {
                SkinPaths.RejectLinks(Root, hiddenPath);
                Directory.Delete(hiddenPath, true);
            }
            var installed = Read(target);
            return new(installed, Replaced: previous.Select(s => s.Record.Id == installed.Id ? s with { ExpectedRevision = installed.Revision } : s).ToArray());
        }
        catch
        {
            // Preserve the previous visible and hidden folders if installation fails midway.
            foreach (var snapshot in previous)
            {
                using var recovery = Materialize(snapshot.Record);
                Commit(recovery, Resolve(snapshot.Record));
            }
            throw;
        }
    }
    public override SkinRecord Replace(SkinRecord skin, SkinWorkspace workspace)
    {
        RequireWritable(skin);
        var path = Resolve(skin);
        if (!Directory.Exists(path)) throw new IOException("Skin was removed externally. Refresh and try again.");
        if (Read(path, skin.Hidden).Revision != skin.Revision) throw new IOException("Skin changed externally. Refresh before editing it.");
        Commit(workspace, path);
        return Read(path, skin.Hidden);
    }
    private void Commit(SkinWorkspace workspace, string target)
    {
        SkinPaths.RejectLinks(Root, target);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var stage = Path.Combine(Path.GetDirectoryName(target)!, ".osm-stage-" + Guid.NewGuid().ToString("N"));
        var backup = Path.Combine(recoveryRoot, Guid.NewGuid().ToString("N"));
        var rollback = Path.Combine(Path.GetDirectoryName(target)!, ".osm-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        try
        {
            foreach (var file in workspace.Files)
            {
                var output = Path.Combine(stage, SkinPaths.Virtual(file.Key));
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.Copy(file.Value, output);
            }
            if (Directory.Exists(target))
            {
                // Stage a recoverable copy before replacing; recovery root may be on another volume.
                Directory.CreateDirectory(recoveryRoot);
                CopyDirectory(target, backup);
                Directory.Move(target, rollback);
            }
            try { Directory.Move(stage, target); }
            catch { if (Directory.Exists(rollback) && !Directory.Exists(target)) Directory.Move(rollback, target); throw; }
            if (Directory.Exists(rollback)) Directory.Delete(rollback, true);
        }
        finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
    }
    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in SkinPaths.Enumerate(source))
        {
            var path = Path.Combine(target, file.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Copy(file.Value, path);
        }
    }
    public override SkinRecord Rename(SkinRecord skin, string name)
    {
        SkinPaths.ValidateName(name);
        EnsureCurrent(skin);
        var old = Resolve(skin);
        var target = Path.Combine(Path.GetDirectoryName(old)!, name);
        if (Directory.Exists(target)) throw new IOException("A skin with this name already exists.");
        using var workspace = Materialize(skin);
        workspace.SetName(name);
        Replace(skin, workspace);
        Directory.Move(old, target);
        return Read(target, skin.Hidden);
    }
    public override SkinSnapshot Delete(SkinRecord skin)
    {
        EnsureCurrent(skin);
        var source = Resolve(skin);
        var trash = Path.Combine(recoveryRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(recoveryRoot);
        CopyDirectory(source, trash);
        Directory.Delete(source, true);
        return new(skin with { Files = SkinPaths.Enumerate(trash) }, true);
    }
    public override SkinRecord Restore(SkinSnapshot snapshot)
    {
        var target = Resolve(snapshot.Record);
        if (snapshot.ExpectedRevision != null && Directory.Exists(target) && Read(target).Revision != snapshot.ExpectedRevision)
            throw new IOException("Cannot undo: this skin has changed since the operation.");
        if (snapshot.Deleted && Directory.Exists(target)) throw new IOException("Cannot undo: another skin now occupies the original folder.");
        using var workspace = Materialize(snapshot.Record);
        Commit(workspace, target);
        return Read(target, snapshot.Record.Hidden);
    }
    public override SkinRecord SetHidden(SkinRecord skin, bool hidden)
    {
        EnsureCurrent(skin);
        var source = Resolve(skin);
        var target = Path.Combine(Root, hidden ? "HiddenSkins" : "Skins", skin.Name);
        if (skin.Hidden == hidden) return skin;
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        Directory.Move(source, target);
        return Read(target, hidden);
    }
}
