namespace OsuSkinMixer.Storage;

using System.Collections;

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
        var files = new StableSkinFiles(path);
        var modified = Directory.GetLastWriteTimeUtc(path);
        string author = "Unknown";
        string iniRevision = "missing";
        if (files.TryGetValue("skin.ini", out var ini))
        {
            author = File.ReadLines(ini).FirstOrDefault(l => l.Split(':')[0].Trim().Equals("Author", StringComparison.OrdinalIgnoreCase))?.Split(':', 2).Last().Trim() ?? author;
            var info = new FileInfo(ini);
            iniRevision = $"{info.Length}:{info.LastWriteTimeUtc.Ticks}";
        }
        // Match stable's historical cheap change detection. File mappings are indexed only if a
        // consumer actually needs them; launch should not walk every asset in every skin.
        var revision = $"{modified.Ticks}:{iniRevision}";
        return new(SkinPaths.Identity(path), Path.GetFileName(path), author, files, revision, modified, hidden);
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
    public override InstallResult DuplicateWithResult(SkinRecord skin, string name, bool overwriteExisting = false)
    {
        EnsureCurrent(skin);
        using var workspace = Materialize(skin);
        workspace.SetName(name);
        // Preserve stable's historical behaviour: same-name copies replace the target.
        return Install(workspace, overwriteExisting: true);
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
            previous.Add(new(record with { Files = new StableSkinFiles(recovery) }, path == hiddenPath));
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
                // Keep the original beside the destination only for atomic rollback. Callers that
                // support undo already own their single recovery snapshot.
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
        return new(skin with { Files = new StableSkinFiles(trash) }, true);
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

    /// <summary>
    /// Stable files already have meaningful paths. Resolve common individual lookups directly and
    /// build the recursive, case-insensitive index only for operations that need the whole skin.
    /// </summary>
    private sealed class StableSkinFiles : IReadOnlyDictionary<string, string>
    {
        private readonly string root;
        private readonly Lazy<Dictionary<string, string>> index;

        public StableSkinFiles(string root)
        {
            this.root = SkinPaths.Canonical(root);
            index = new Lazy<Dictionary<string, string>>(() => SkinPaths.Enumerate(this.root), true);
        }

        public bool TryGetValue(string key, out string value)
        {
            key = SkinPaths.Virtual(key);
            if (index.IsValueCreated)
                return index.Value.TryGetValue(key, out value!);

            var direct = Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(direct))
            {
                SkinPaths.RejectLinks(root, direct);
                value = direct;
                return true;
            }

            // Windows and normal macOS volumes already perform case-insensitive direct lookups.
            // On case-sensitive platforms, inspect only the requested path's parent directories;
            // a missing optional texture must not trigger a recursive index of the entire skin.
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                var current = root;
                foreach (var segment in key.Split('/'))
                {
                    if (!Directory.Exists(current)) break;
                    var match = Directory.EnumerateFileSystemEntries(current)
                        .FirstOrDefault(path => Path.GetFileName(path).Equals(segment, StringComparison.OrdinalIgnoreCase));
                    if (match == null) break;
                    current = match;
                }
                if (File.Exists(current) && Path.GetRelativePath(root, current).Replace('\\', '/').Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    SkinPaths.RejectLinks(root, current);
                    value = current;
                    return true;
                }
            }

            value = null!;
            return false;
        }

        public string this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
        public bool ContainsKey(string key) => TryGetValue(key, out _);
        public IEnumerable<string> Keys => index.Value.Keys;
        public IEnumerable<string> Values => index.Value.Values;
        public int Count => index.Value.Count;
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => index.Value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
