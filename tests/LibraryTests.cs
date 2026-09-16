using System.IO.Compression;
using NUnit.Framework;
using OsuSkinMixer.Storage;
using Realms;

namespace OsuSkinMixer.Tests;

[TestFixture]
public class LibraryTests
{
    private string root = null!;
    private string backups = null!;
    [SetUp] public void Setup()
    {
        root = Path.Combine(Path.GetTempPath(), "osm-tests-" + Guid.NewGuid().ToString("N"));
        backups = Path.Combine(root, "backups");
        Directory.CreateDirectory(Path.Combine(root, "Skins"));
    }
    [TearDown] public void Cleanup() { Realm.DeleteRealm(LazerSkinLibrary.Configuration(Path.Combine(root, "client.realm"), 51, false)); Directory.Delete(root, true); }
    private static SkinWorkspace Workspace(string name = "Test")
    {
        var workspace = new SkinWorkspace();
        File.WriteAllText(Path.Combine(workspace.DirectoryPath, "Skin.INI"), $"[General]\nName: {name}\nAuthor: Tests\n");
        File.WriteAllText(Path.Combine(workspace.DirectoryPath, "hitcircle.png"), "synthetic image bytes");
        Directory.CreateDirectory(Path.Combine(workspace.DirectoryPath, "sub"));
        File.WriteAllText(Path.Combine(workspace.DirectoryPath, "sub", "sound.wav"), "synthetic sound bytes");
        return workspace;
    }
    private LazerSkinLibrary Lazer(ulong version = 51)
    {
        using (Realm.GetInstance(LazerSkinLibrary.Configuration(Path.Combine(root, "client.realm"), version, false))) { }
        return new LazerSkinLibrary(root, backups);
    }
    [Test] public void StableDiscoveryExportHideDeleteRestore()
    {
        var library = new StableSkinLibrary(root, backups);
        using var workspace = Workspace();
        var skin = library.Install(workspace).Skin;
        Assert.That(library.Load(), Has.Count.EqualTo(1));
        Assert.That(skin.Files.ContainsKey("skin.ini"));
        var archive = Path.Combine(root, "export.osk"); library.Export(skin, archive);
        using (var zip = ZipFile.OpenRead(archive)) Assert.That(zip.Entries, Has.Count.EqualTo(3));
        var hidden = library.SetHidden(skin, true);
        Assert.That(hidden.Hidden); Assert.That(library.Load().Single().Id, Is.EqualTo(hidden.Id));
        var snapshot = library.Delete(hidden); Assert.That(library.Load(), Is.Empty);
        var restored = library.Restore(snapshot); Assert.That(restored.Hidden);
        Assert.That(File.ReadAllText(restored.Files["sub/sound.wav"]), Is.EqualTo("synthetic sound bytes"));
    }
    [Test] public void StableDuplicateRenameAndReplaceDoNotMutateSource()
    {
        var library = new StableSkinLibrary(root, backups);
        using var workspace = Workspace();
        var original = library.Install(workspace).Skin;
        var duplicate = library.Duplicate(original, "Copy");
        Assert.That(duplicate.Id, Is.Not.EqualTo(original.Id));
        var renamed = library.Rename(duplicate, "Renamed");
        Assert.That(renamed.Name, Is.EqualTo("Renamed"));
        using var edit = library.Materialize(original); edit.SetName("Metadata only");
        var replaced = library.Replace(original, edit);
        Assert.That(replaced.Id, Is.EqualTo(original.Id));
        Assert.Throws<IOException>(() => library.Install(workspace));
    }
    [TestCase("../escape")][TestCase("/absolute")][TestCase("C:\\escape")][TestCase("sub/../../escape")]
    public void UnsafePathsRejected(string path) => Assert.Throws<InvalidDataException>(() => SkinPaths.Virtual(path));
    [Test] public void WorkspacesAreUniqueAndCaseInsensitive()
    {
        using var first = Workspace(); using var second = Workspace();
        Assert.That(first.DirectoryPath, Is.Not.EqualTo(second.DirectoryPath));
        Assert.That(first.Files.ContainsKey("skin.ini"));
        first.SetName("Renamed"); Assert.That(first.Metadata().Name, Is.EqualTo("Renamed"));
    }
    [TestCase(51ul)][TestCase(52ul)][TestCase(77ul)]
    public void LazerRoundTripIdentityHashReuseDeleteAndUndo(ulong version)
    {
        var library = Lazer(version);
        using var workspace = Workspace();
        var installed = library.Install(workspace);
        Assert.That(library.SchemaVersion, Is.EqualTo(version));
        Assert.That(library.Load(), Has.Count.EqualTo(1));
        var skin = installed.Skin;
        Assert.That(Guid.TryParse(skin.Id, out _));
        foreach (var file in skin.Files.Values) Assert.That(SkinPaths.Hash(file), Is.EqualTo(Path.GetFileName(file)));
        var reused = library.Install(workspace); Assert.That(reused.Reused); Assert.That(reused.Skin.Id, Is.EqualTo(skin.Id));
        var snapshot = library.Delete(skin); Assert.That(library.Load(), Is.Empty);
        Assert.That(skin.Files.Values.All(File.Exists));
        var restored = library.Restore(snapshot); Assert.That(restored.Id, Is.EqualTo(skin.Id));
        var renamed = library.Rename(restored, "Renamed"); Assert.That(renamed.Id, Is.EqualTo(skin.Id));
        Assert.That(renamed.Name, Is.EqualTo("Renamed"));
        var duplicate = library.Duplicate(renamed, "Renamed"); Assert.That(duplicate.Id, Is.Not.EqualTo(skin.Id));
        Assert.That(Directory.GetFiles(backups, "*.realm", SearchOption.AllDirectories), Has.Length.EqualTo(5));
    }
    [Test] public void LazerWriteCreatesVerifiedPreMutationBackup()
    {
        var library = Lazer(77); using var workspace = Workspace();
        library.Install(workspace);
        var backup = Directory.GetFiles(backups, "*.realm", SearchOption.AllDirectories).Single();
        using var copy = Realm.GetInstance(LazerSkinLibrary.Configuration(backup, 77, true));
        Assert.That(copy.All<LazerSkin>().Count(), Is.Zero);
        Assert.That(library.Load(), Has.Count.EqualTo(1));
    }
    [Test] public void MissingContentIsDegradedAndBlocksExportAndUndo()
    {
        var library = Lazer(); using var workspace = Workspace();
        var skin = library.Install(workspace).Skin; var snapshot = library.Delete(skin);
        File.Delete(skin.Files["hitcircle.png"]);
        Assert.Throws<IOException>(() => library.Restore(snapshot));
        Assert.Throws<InvalidDataException>(() => library.Export(skin, Path.Combine(root, "bad.osk")));
    }
    [Test] public void LazerLoadStillReportsMissingContent()
    {
        var library = Lazer(); using var workspace = Workspace();
        var skin = library.Install(workspace).Skin;
        File.Delete(skin.Files["hitcircle.png"]);
        Assert.That(library.Load().Single().Problem, Is.Not.Null);
    }
    [Test] public void LazerLoadStillRejectsLinkedContent()
    {
        if (OperatingSystem.IsWindows()) Assert.Ignore("Creating symbolic links may require additional privileges on Windows.");
        var library = Lazer(); using var workspace = Workspace();
        var skin = library.Install(workspace).Skin;
        var linked = skin.Files["hitcircle.png"];
        File.Delete(linked);
        File.CreateSymbolicLink(linked, skin.Files["skin.ini"]);
        Assert.That(library.Load().Single().Problem, Is.Not.Null);
    }
    [Test] public void ProtectedAndNonLegacySkinsCannotBeWritten()
    {
        var library = Lazer(); using var workspace = Workspace(); var skin = library.Install(workspace).Skin;
        using (var realm = Realm.GetInstance(LazerSkinLibrary.Configuration(Path.Combine(root, "client.realm"), 51, false)))
            realm.Write(() => realm.Find<LazerSkin>(Guid.Parse(skin.Id))!.Protected = true);
        Assert.That(library.Load(), Is.Empty);
        Assert.Throws<InvalidOperationException>(() => library.Delete(skin));
    }
    [Test] public void ExternalChangesRejectStaleWrite()
    {
        var library = Lazer(); using var workspace = Workspace(); var skin = library.Install(workspace).Skin;
        var renamed = library.Rename(skin, "Changed");
        Assert.Throws<IOException>(() => library.Replace(skin, workspace));
        Assert.That(library.Load().Single().Name, Is.EqualTo(renamed.Name));
    }
    [Test] public void IncompatibleSchemaFailsClosed()
    {
        var path = Path.Combine(root, "client.realm");
        using (Realm.GetInstance(new RealmConfiguration(path) { SchemaVersion = 77, Schema = new[] { typeof(IncompatibleSkin) } })) { }
        var library = new LazerSkinLibrary(root, backups);
        Assert.Throws<InvalidDataException>(() => library.Load());
        using var workspace = Workspace();
        Assert.Throws<InvalidDataException>(() => library.Install(workspace));
    }
    [Test] public void UnrelatedTablesSurviveWritesAndBackups()
    {
        var path = Path.Combine(root, "client.realm");
        var config = new RealmConfiguration(path) { SchemaVersion = 52,
            Schema = new[] { typeof(LazerSkin), typeof(LazerFile), typeof(RealmNamedFileUsage), typeof(UnrelatedData) } };
        using (var realm = Realm.GetInstance(config)) realm.Write(() => realm.Add(new UnrelatedData { ID = "keep", Value = "precious data" }));
        var library = new LazerSkinLibrary(root, backups);
        using var workspace = Workspace(); library.Install(workspace);
        using (var realm = Realm.GetInstance(config)) Assert.That(realm.Find<UnrelatedData>("keep")!.Value, Is.EqualTo("precious data"));
        var backup = Directory.GetFiles(backups, "*.realm", SearchOption.AllDirectories).Single();
        var read = new RealmConfiguration(backup) { IsReadOnly = true, SchemaVersion = 52, Schema = config.Schema };
        using (var realm = Realm.GetInstance(read)) Assert.That(realm.Find<UnrelatedData>("keep")!.Value, Is.EqualTo("precious data"));
    }
    [Test] public void FailedSkinCommitLeavesFilesRegisteredAndNoSkin()
    {
        var library = Lazer(); using var workspace = Workspace();
        library.BeforeSkinCommit = () => throw new IOException("Injected skin-commit failure");
        Assert.Throws<IOException>(() => library.Install(workspace));
        Assert.That(library.Load(), Is.Empty);
        using var realm = Realm.GetInstance(LazerSkinLibrary.Configuration(Path.Combine(root, "client.realm"), 51, true));
        Assert.That(realm.All<LazerFile>().Count(), Is.EqualTo(3));
        Assert.That(realm.All<LazerFile>().ToList().All(f => File.Exists(library.BlobPath(f.Hash))));
    }
    [Test] public void SameIniButDifferentAssetIsNotReused()
    {
        var library = Lazer(); using var first = Workspace(); using var second = Workspace();
        var original = library.Install(first).Skin;
        File.WriteAllText(second.Files["hitcircle.png"], "different bytes");
        var installed = library.Install(second);
        Assert.That(installed.Reused, Is.False); Assert.That(installed.Skin.Id, Is.Not.EqualTo(original.Id));
    }
    [Test] public void SoftDeletedDuplicateIsReusedWithoutRemovingSharedBlobs()
    {
        var library = Lazer(); using var workspace = Workspace();
        var original = library.Install(workspace).Skin;
        var duplicate = library.Duplicate(original, "copy");
        library.Delete(original);
        Assert.That(File.Exists(duplicate.Files["hitcircle.png"]));
        var result = library.Install(workspace);
        Assert.That(result.Reused); Assert.That(result.Skin.Id, Is.EqualTo(original.Id));
    }
    [Test] public void CleanupInvalidatesUndo()
    {
        var library = Lazer(); using var workspace = Workspace(); var skin = library.Install(workspace).Skin;
        var snapshot = library.Delete(skin);
        using (var realm = Realm.GetInstance(LazerSkinLibrary.Configuration(Path.Combine(root, "client.realm"), 51, false)))
            realm.Write(() => realm.Remove(realm.Find<LazerSkin>(Guid.Parse(skin.Id))!));
        Assert.Throws<IOException>(() => library.Restore(snapshot));
    }
    [Test] public void MalformedMappingsDoNotEscapeWorkspace()
    {
        var library = Lazer(); using var workspace = Workspace(); var skin = library.Install(workspace).Skin;
        using (var realm = Realm.GetInstance(LazerSkinLibrary.Configuration(Path.Combine(root, "client.realm"), 51, false)))
            realm.Write(() => realm.Find<LazerSkin>(Guid.Parse(skin.Id))!.Files[0].Filename = "../outside");
        var degraded = library.Load().Single(); Assert.That(degraded.Problem, Is.Not.Null);
        Assert.Throws<InvalidDataException>(() => library.Materialize(degraded));
        using var preview = library.Materialize(degraded, true);
        Assert.That(preview.Files.Keys, Does.Not.Contain("../outside"));
    }
    [TestCase(false)][TestCase(true)]
    public void MixerOverwritePreservesIdentityAndUndoRestoresOriginal(bool lazer)
    {
        ISkinLibrary library = lazer ? Lazer() : new StableSkinLibrary(root, backups);
        using var original = Workspace("Same name");
        var before = library.Install(original).Skin;
        using var replacement = Workspace("Same name");
        File.WriteAllText(replacement.Files["hitcircle.png"], "replacement pixels");
        File.WriteAllText(Path.Combine(replacement.DirectoryPath, "credits.ini"), "replacement credits");
        var result = library.Install(replacement, overwriteExisting: true);
        Assert.That(result.Skin.Id, Is.EqualTo(before.Id));
        Assert.That(library.Load(), Has.Count.EqualTo(1));
        Assert.That(File.ReadAllText(result.Skin.Files["hitcircle.png"]), Is.EqualTo("replacement pixels"));
        Assert.That(result.Skin.Files.ContainsKey("credits.ini"));
        library.UndoInstall(result);
        var restored = library.Load().Single();
        Assert.That(restored.Id, Is.EqualTo(before.Id));
        Assert.That(File.ReadAllText(restored.Files["hitcircle.png"]), Is.EqualTo("synthetic image bytes"));
        Assert.That(restored.Files.ContainsKey("credits.ini"), Is.False);
    }
    [Test] public void StableMixerOverwriteAlsoReplacesHiddenSkinAndUndoRestoresBoth()
    {
        var library = new StableSkinLibrary(root, backups);
        using var original = Workspace("Same name");
        var hidden = library.SetHidden(library.Install(original).Skin, true);
        // Represents a pre-existing visible/hidden conflict, which stable's mixer has always replaced.
        Directory.CreateDirectory(Path.Combine(root, "Skins", "Same name"));
        File.WriteAllText(Path.Combine(root, "Skins", "Same name", "skin.ini"), "[General]\nName: Same name\n");
        using var replacement = Workspace("Same name");
        var result = library.Install(replacement, overwriteExisting: true);
        Assert.That(library.Load(), Has.Count.EqualTo(1)); Assert.That(library.Load().Single().Hidden, Is.False);
        library.UndoInstall(result);
        Assert.That(library.Load(), Has.Count.EqualTo(2));
        Assert.That(library.Load().Single(s => s.Hidden).Id, Is.EqualTo(hidden.Id));
    }
    [Test] public void AmbiguousLazerOverwriteDoesNotChooseAnArbitrarySkin()
    {
        var library = Lazer(); using var original = Workspace("Same name");
        var first = library.Install(original).Skin;
        library.Duplicate(first, "Same name");
        Assert.Throws<InvalidOperationException>(() => library.Install(original, overwriteExisting: true));
        Assert.That(library.Load(), Has.Count.EqualTo(2));
    }
    [Test] public void UnknownPropertiesOnKnownTablesAllowReadButBlockWritesWithoutMigration()
    {
        var path = Path.Combine(root, "client.realm"); var id = Guid.NewGuid();
        var config = new RealmConfiguration(path) { SchemaVersion = 77,
            Schema = new[] { typeof(ExtendedSkin), typeof(LazerFile), typeof(RealmNamedFileUsage) } };
        using (var realm = Realm.GetInstance(config)) realm.Write(() => realm.Add(new ExtendedSkin { ID = id, Protected = true, FutureProperty = "keep this too" }));
        var library = new LazerSkinLibrary(root, backups);
        Assert.That(library.Load(), Is.Empty);
        Assert.That(library.WriteRestriction, Is.Not.Null);
        using var workspace = Workspace(); Assert.Throws<InvalidDataException>(() => library.Install(workspace));
        using (var realm = Realm.GetInstance(config)) Assert.That(realm.Find<ExtendedSkin>(id)!.FutureProperty, Is.EqualTo("keep this too"));
    }
    [Test] public void OptionalCopiedRealDatabasePreservesAllOtherTableCounts()
    {
        var source = Environment.GetEnvironmentVariable("OSM_REALM_FIXTURE");
        if (string.IsNullOrEmpty(source)) Assert.Ignore("Set OSM_REALM_FIXTURE to a disposable copy of client.realm for this additional integration check.");
        var path = Path.Combine(root, "client.realm");
        File.Copy(source!, path); // All writes remain confined to this test's private copy.
        var library = new LazerSkinLibrary(root, backups);
        _ = library.Load();
        Assert.That(library.WriteRestriction, Is.Null);
        Dictionary<string, int> Counts()
        {
            using var realm = Realm.GetInstance(new RealmConfiguration(path) { IsReadOnly = true, IsDynamic = true, SchemaVersion = library.SchemaVersion });
            return realm.Schema.Where(s => s.BaseType == Realms.Schema.ObjectSchema.ObjectType.RealmObject)
                .ToDictionary(s => s.Name, s => realm.DynamicApi.All(s.Name).Count());
        }
        var before = Counts();
        using var workspace = Workspace("isolated compatibility probe");
        library.Install(workspace);
        var after = Counts();
        Assert.That(after.Keys, Is.EquivalentTo(before.Keys));
        foreach (var table in before.Where(p => p.Key != "Skin" && p.Key != "File"))
            Assert.That(after[table.Key], Is.EqualTo(table.Value), table.Key);
        Assert.That(after["Skin"], Is.EqualTo(before["Skin"] + 1));
        Assert.That(after["File"], Is.EqualTo(before["File"] + 3));
    }
}
