using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Realms;

namespace OsuSkinMixer.Storage;

/// <summary>Local, legacy-skin adapter. Never migrates a Realm or removes content-addressed files.</summary>
public sealed class LazerSkinLibrary : SkinLibrary
{
    private const string UnsupportedSchemaMarker = "OsuSkinMixer.UnsupportedLazerSchema";
    public const string LegacyType = "osu.Game.Skinning.LegacySkin, osu.Game";
    private static readonly object databaseGate = new();
    private readonly string backupRoot;
    public ulong SchemaVersion { get; private set; }
    public bool KnownSchema => SchemaVersion is 51 or 52;
    private string? writeRestriction;
    private bool allowUnsupportedSchemaWrites;
    public override string? WriteRestriction => allowUnsupportedSchemaWrites ? null : writeRestriction;
    public override OsuClientKind Kind => OsuClientKind.Lazer;
    public override string Status => $"osu!lazer · Realm schema {SchemaVersion}" +
        (WriteRestriction != null ? " · read-only: " + WriteRestriction : writeRestriction != null ? " · unsupported schema · writes allowed by user" : KnownSchema ? " · experimental direct access · verified backups" : " · untested schema · experimental direct access");
    private string DatabasePath => Path.Combine(Root, "client.realm");
    internal Action? BeforeSkinCommit { get; set; }

    public LazerSkinLibrary(string root, string backupRoot) : base(root)
    {
        this.backupRoot = Path.Combine(backupRoot, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(SkinPaths.Identity(root)))));
    }
    public void AllowUnsupportedSchemaWrites() => allowUnsupportedSchemaWrites = true;
    public static bool IsUnsupportedSchemaError(Exception exception)
        => exception is InvalidDataException && exception.Data.Contains(UnsupportedSchemaMarker);
    private static InvalidDataException UnsupportedSchemaError(string message, Exception? innerException = null)
    {
        var exception = new InvalidDataException(message, innerException);
        exception.Data[UnsupportedSchemaMarker] = true;
        return exception;
    }
    public void InspectCompatibility()
    {
        lock (databaseGate)
        {
            using (var realm = OpenRead()) { }
            InspectWriteSchema();
        }
    }
    public static RealmConfiguration Configuration(string path, ulong version, bool readOnly) => new(path)
    {
        SchemaVersion = version,
        IsReadOnly = readOnly,
        Schema = new[] { typeof(LazerSkin), typeof(RealmNamedFileUsage), typeof(LazerFile) },
        MigrationCallback = readOnly ? null : (_, _) => throw new InvalidOperationException("Refusing to migrate the osu! database."),
    };

    private Realm OpenRead(string? path = null)
    {
        path ??= DatabasePath;
        if (!File.Exists(path)) throw new FileNotFoundException("client.realm was not found.", path);
        ulong version = SchemaVersion == 0 ? 52 : SchemaVersion;
        try
        {
            var realm = Realm.GetInstance(Configuration(path, version, true));
            SchemaVersion = version;
            return realm;
        }
        catch (Exception e)
        {
            // Realm has no public static schema-version reader. Read-only open must never migrate.
            var match = Regex.Match(e.Message, @"Provided schema version \d+ does not equal last set version (\d+)");
            if (!match.Success) throw UnsupportedSchemaError("Incompatible or unreadable lazer database. No changes were made.", e);
            version = ulong.Parse(match.Groups[1].Value);
            try
            {
                var realm = Realm.GetInstance(Configuration(path, version, true));
                SchemaVersion = version;
                return realm;
            }
            catch (Exception inner) { throw UnsupportedSchemaError("Unsupported lazer schema version.", inner); }
        }
    }

    public string BlobPath(string hash) => BlobPath(hash, null);
    private string BlobPath(string hash, ISet<string>? validatedPaths)
    {
        if (hash.Length != 64 || hash.Any(c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
            throw new InvalidDataException("Invalid lazer content hash.");
        var path = Path.Combine(Root, "files", hash[..1], hash[..2], hash);
        SkinPaths.RejectLinks(Root, path, validatedPaths);
        return path;
    }

    private sealed class LoadContext
    {
        public DateTime DatabaseModified { get; }
        public Dictionary<string, (string Path, bool Exists)> Blobs { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ValidatedPaths { get; } = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        public LoadContext(string databasePath) => DatabaseModified = File.GetLastWriteTimeUtc(databasePath);
    }

    private (string Path, bool Exists) InspectBlob(string hash, LoadContext context)
    {
        if (context.Blobs.TryGetValue(hash, out var cached)) return cached;
        var path = BlobPath(hash, context.ValidatedPaths);
        var inspected = (path, File.Exists(path));
        context.Blobs.Add(hash, inspected);
        return inspected;
    }

    public override IReadOnlyList<SkinRecord> Load()
    {
        lock (databaseGate)
        {
            SkinRecord[] records;
            var context = new LoadContext(DatabasePath);
            using (var realm = OpenRead())
                records = realm.All<LazerSkin>().Where(s => !s.Protected && !s.DeletePending).ToList().Select(s => Detach(s, context)).ToArray();
            InspectWriteSchema();
            return records;
        }
    }
    private void InspectWriteSchema()
    {
        using var actual = Realm.GetInstance(new RealmConfiguration(DatabasePath) { IsReadOnly = true, IsDynamic = true, SchemaVersion = SchemaVersion });
        writeRestriction = null;
        foreach (var expected in Configuration(DatabasePath, SchemaVersion, true).Schema!)
        {
            var stored = actual.Schema.SingleOrDefault(s => s.Name == expected.Name);
            if (stored == null || stored.BaseType != expected.BaseType) { writeRestriction = "The skin table structure is unsupported."; return; }
            foreach (var property in stored.Where(p => p.LinkOriginPropertyName == null))
            {
                if (!expected.TryFindProperty(property.Name, out var mapped) || mapped.Type != property.Type ||
                    mapped.ObjectType != property.ObjectType || mapped.IsPrimaryKey != property.IsPrimaryKey || mapped.IndexType != property.IndexType)
                {
                    writeRestriction = $"Unsupported property {stored.Name}.{property.Name}. A storage adapter update is required; overrides cannot migrate or remove fields.";
                    return;
                }
            }
        }
    }
    private SkinRecord Detach(LazerSkin skin) => Detach(skin, new LoadContext(DatabasePath));
    private SkinRecord Detach(LazerSkin skin, LoadContext context)
    {
        bool legacy = string.IsNullOrEmpty(skin.InstantiationInfo) || skin.InstantiationInfo == LegacyType;
        var usages = skin.Files.Select(usage => (usage.Filename, Hash: usage.File?.Hash)).ToArray();
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? problem = null;
        foreach (var usage in usages)
        {
            try
            {
                if (usage.Hash == null) throw new InvalidDataException("Skin file mapping has no content hash.");
                var blob = InspectBlob(usage.Hash, context);
                if (!files.TryAdd(SkinPaths.Virtual(usage.Filename), blob.Path))
                    problem = "Skin has ambiguous case-insensitive filenames.";
                if (!blob.Exists) problem = "Skin has missing content files. Repair or re-import it in osu! before exporting or editing.";
            }
            catch (Exception e) when (e is InvalidDataException or NullReferenceException) { problem = "Skin contains invalid file mappings."; }
        }
        if (legacy && !files.ContainsKey("skin.ini")) problem ??= "Skin has no skin.ini; editing is disabled.";
        var revision = skin.Hash + "|" + skin.Name + "|" + skin.Creator + "|" + skin.DeletePending + "|" +
            string.Join('|', usages.Select(f => f.Filename + ":" + f.Hash).Order());
        return new(skin.ID.ToString(), skin.Name, skin.Creator, files, revision, context.DatabaseModified,
            IsLegacy: legacy, Problem: problem);
    }

    public override SkinWorkspace Materialize(SkinRecord skin, bool allowDegraded = false)
    {
        if (!allowDegraded)
            foreach (var file in skin.Files)
                if (!File.Exists(file.Value) || SkinPaths.Hash(file.Value) != Path.GetFileName(file.Value))
                    throw new InvalidDataException("A skin content file is missing or corrupt. Repair the skin in osu! first.");
        return base.Materialize(skin, allowDegraded);
    }
    public static string AggregateHash(IReadOnlyDictionary<string, string> files)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        // Mirrors SkinImporter.GetHashableFiles: concatenate ini/json bytes ordered by standardised filename.
        foreach (var file in files.Where(p => Path.GetExtension(p.Key).Equals(".ini", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(p.Key).Equals(".json", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.Key.Replace('\\', '/')))
        {
            using var stream = File.OpenRead(file.Value);
            var buffer = new byte[81920]; int count;
            while ((count = stream.Read(buffer)) != 0) hash.AppendData(buffer, 0, count);
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private T Write<T>(string operation, Func<Realm, T> action)
    {
        lock (databaseGate)
        {
            SkinPaths.RejectLinks(Root, DatabasePath);
            using (var validation = OpenRead()) { }
            InspectWriteSchema();
            if (WriteRestriction != null) throw new InvalidDataException(WriteRestriction);
            Directory.CreateDirectory(backupRoot);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(backupRoot, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var backup = Path.Combine(backupRoot, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff") + ".realm");
            using (var read = OpenRead()) read.WriteCopy(Configuration(backup, SchemaVersion, false));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(backup, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            using (var verification = OpenRead(backup)) { _ = verification.All<LazerSkin>().Count(); }
            // Prune only validated database copies, never the live database or its blobs.
            foreach (var old in Directory.GetFiles(backupRoot, "*.realm").OrderDescending().Skip(5)) File.Delete(old);
            using (var read = OpenRead()) { } // Revalidate schema immediately before opening writable.
            using var realm = Realm.GetInstance(Configuration(DatabasePath, SchemaVersion, false));
            return action(realm);
        }
    }
    private Dictionary<string, string> StageFiles(Realm realm, SkinWorkspace workspace)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var file in workspace.Files)
            {
                var hash = SkinPaths.Hash(file.Value);
                var target = BlobPath(hash);
                if (File.Exists(target))
                {
                    if (SkinPaths.Hash(target) != hash) throw new InvalidDataException("Existing lazer blob is corrupt; refusing to overwrite it.");
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    var temporary = target + ".osm-" + Guid.NewGuid().ToString("N");
                    try
                    {
                        File.Copy(file.Value, temporary);
                        if (SkinPaths.Hash(temporary) != hash) throw new IOException("Staged content checksum mismatch.");
                        File.Move(temporary, target);
                    }
                    finally { if (File.Exists(temporary)) File.Delete(temporary); }
                }
                result.Add(SkinPaths.Virtual(file.Key), hash);
            }
            return result;
        }
        finally
        {
            // Even a partially failed staging operation leaves created blobs registered for osu!'s GC.
            if (result.Count > 0) AddFiles(realm, result);
        }
    }
    private static void AddFiles(Realm realm, Dictionary<string, string> files)
    {
        realm.Write(() =>
        {
            foreach (var hash in files.Values.Distinct())
                if (realm.Find<LazerFile>(hash) == null) realm.Add(new LazerFile { Hash = hash });
        });
    }
    private static void Apply(Realm realm, LazerSkin skin, Dictionary<string, string> files, string name, string author, string hash)
    {
        skin.Name = name; skin.Creator = author; skin.Hash = hash;
        skin.Files.Clear();
        foreach (var file in files) skin.Files.Add(new RealmNamedFileUsage { Filename = file.Key, File = realm.Find<LazerFile>(file.Value)! });
    }
    public override InstallResult Install(SkinWorkspace workspace, bool overwriteExisting = false) => Install(workspace, false, overwriteExisting);
    private InstallResult Install(SkinWorkspace workspace, bool forceNew, bool overwriteExisting)
    {
        var metadata = workspace.Metadata();
        return Write("Install skin", realm =>
        {
            LazerSkin? replacement = null;
            SkinSnapshot? snapshot = null;
            if (overwriteExisting)
            {
                var matches = realm.All<LazerSkin>().Where(s => !s.Protected && !s.DeletePending && s.Name == metadata.Name).ToList();
                if (matches.Count > 1) throw new InvalidOperationException($"More than one osu!lazer skin is named \"{metadata.Name}\". Rename or delete the duplicates in osu! before using this name.");
                replacement = matches.SingleOrDefault();
                if (replacement != null)
                {
                    var record = Detach(replacement);
                    FindWritable(realm, record);
                    snapshot = new SkinSnapshot(record);
                }
            }
            var files = StageFiles(realm, workspace); var hash = AggregateHash(workspace.Files);
            var existing = forceNew || replacement != null ? null : realm.All<LazerSkin>().Where(s => s.Hash == hash && !s.Protected).ToList().FirstOrDefault(s =>
                s.Files.Count == files.Count && s.Files.All(f => files.TryGetValue(f.Filename, out var h) && h == f.File.Hash));
            if (existing != null)
            {
                realm.Write(() => existing.DeletePending = false);
                return new InstallResult(Detach(existing), true);
            }
            BeforeSkinCommit?.Invoke();
            if (replacement != null)
            {
                realm.Write(() => Apply(realm, replacement, files, metadata.Name, metadata.Author, hash));
                var updated = Detach(replacement);
                return new InstallResult(updated, Replaced: new[] { snapshot! with { ExpectedRevision = updated.Revision } });
            }
            var skin = new LazerSkin { ID = Guid.NewGuid(), InstantiationInfo = LegacyType };
            realm.Write(() => { realm.Add(skin); Apply(realm, skin, files, metadata.Name, metadata.Author, hash); });
            return new InstallResult(Detach(skin));
        });
    }
    private LazerSkin FindWritable(Realm realm, SkinRecord record, bool checkRevision = true)
    {
        RequireWritable(record);
        var skin = realm.Find<LazerSkin>(Guid.Parse(record.Id)) ?? throw new IOException("Skin was removed by osu! cleanup. This operation can no longer be completed or undone.");
        if (skin.Protected || !(string.IsNullOrEmpty(skin.InstantiationInfo) || skin.InstantiationInfo == LegacyType))
            throw new InvalidOperationException("Protected or non-legacy skin cannot be changed.");
        if (checkRevision && Detach(skin).Revision != record.Revision) throw new IOException("Skin changed externally. Refresh and try again.");
        return skin;
    }
    public override SkinRecord Replace(SkinRecord skin, SkinWorkspace workspace) => Write("Replace skin", realm =>
    {
        var target = FindWritable(realm, skin);
        var files = StageFiles(realm, workspace);
        BeforeSkinCommit?.Invoke();
        var metadata = workspace.Metadata(); var hash = AggregateHash(workspace.Files);
        realm.Write(() => Apply(realm, target, files, metadata.Name, metadata.Author, hash));
        return Detach(target);
    });
    public override InstallResult DuplicateWithResult(SkinRecord skin, string name, bool overwriteExisting = false)
    {
        RequireWritable(skin);
        using var workspace = Materialize(skin); workspace.SetName(name);
        // A copy must remain distinct from its source, while an explicitly requested
        // same-name overwrite must preserve the existing target's Realm identity.
        return Install(workspace, true, overwriteExisting);
    }
    public override SkinSnapshot Delete(SkinRecord skin) => Write("Delete skin", realm =>
    {
        var target = FindWritable(realm, skin);
        realm.Write(() => target.DeletePending = true);
        return new SkinSnapshot(skin, true, Detach(target).Revision);
    });
    public override SkinRecord Restore(SkinSnapshot snapshot) => Write("Undo / restore skin", realm =>
    {
        var target = FindWritable(realm, snapshot.Record, false);
        if (snapshot.ExpectedRevision != null && Detach(target).Revision != snapshot.ExpectedRevision)
            throw new IOException("Cannot undo: this skin has changed since the operation.");
        // Do not resurrect blobs removed by the game's garbage collector.
        foreach (var path in snapshot.Record.Files.Values)
            if (!File.Exists(path) || SkinPaths.Hash(path) != Path.GetFileName(path))
                throw new IOException("Undo is no longer available: osu! cleaned up or changed the original content files.");
        var files = snapshot.Record.Files.ToDictionary(p => p.Key, p => Path.GetFileName(p.Value), StringComparer.OrdinalIgnoreCase);
        AddFiles(realm, files);
        var hash = AggregateHash(snapshot.Record.Files);
        realm.Write(() =>
        {
            if (!snapshot.Deleted) Apply(realm, target, files, snapshot.Record.Name, snapshot.Record.Author, hash);
            target.DeletePending = false;
        });
        return Detach(target);
    });
    public override SkinRecord SetHidden(SkinRecord skin, bool hidden) => throw new NotSupportedException("Lazer does not support hidden skin folders. Export an editable .osk instead.");
}
