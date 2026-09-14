namespace OsuSkinMixer.Statics;

using System.IO;
using OsuSkinMixer.Models;
using OsuSkinMixer.Storage;

/// <summary>Client-neutral catalogue facade. All writes go through the owning library.</summary>
public static class OsuData
{
    public static event Action AllSkinsLoaded;
    public static event Action<OsuSkin> SkinAdded;
    public static event Action<OsuSkin> SkinModified;
    public static event Action<OsuSkin> SkinRemoved;
    public static event Action<IEnumerable<OsuSkin>> SkinInfoRequested;
    public static event Action<IEnumerable<OsuSkin>> SkinModifyRequested;
    public static event Action<OsuSkin, OsuSkin> SkinConflictDetected;
    public static bool SweepPaused { get; set; } = true;
    public static ISkinLibrary Library { get; private set; }
    private static readonly object gate = new();
    private static Dictionary<string, OsuSkin> skins = new();
    private static FileSystemWatcher watcher;
    private static DateTime lastRefresh;
    private static volatile bool dirty = true;
    public static OsuSkin[] Skins { get { lock (gate) return skins.Values.OrderBy(s => s.Name).ToArray(); } }

    static OsuData()
    {
        _ = Task.Run(async () =>
        {
            GodotThread.SetThreadSafetyChecksEnabled(false);
            while (true)
            {
                await Task.Delay(1500);
                if (SweepPaused || Operation.IsBusy || Utils.SkinMachine.IsRunning || Library == null) continue;
                if (!dirty && DateTime.UtcNow - lastRefresh < TimeSpan.FromSeconds(15)) continue;
                try { Refresh(); } catch (Exception e) { Settings.Log($"Catalogue refresh deferred: {e.Message}"); }
            }
        });
    }
    public static bool TryLoadSkins()
    {
        if (string.IsNullOrEmpty(Settings.Content.OsuFolder)) return false;
        var root = Settings.Content.OsuFolder;
        ISkinLibrary next;
        if (File.Exists(Path.Combine(root, "client.realm")))
            next = new LazerSkinLibrary(root, Path.Combine(Settings.AppdataFolderPath, "realm-backups")) { ConfirmWrite = LibraryActions.ConfirmWrite };
        else if (Directory.Exists(Path.Combine(root, "Skins")))
            next = new StableSkinLibrary(root, Path.Combine(Settings.AppdataFolderPath, "skin-recovery"));
        else return false;
        Connect(next);
        return true;
    }
    public static void Connect(ISkinLibrary next)
    {
        var root = next.Root;
        var records = next.Load();
        var nextSkins = records.Select(r => new OsuSkin(next, r)).ToDictionary(s => s.Identity);
        var nextWatcher = new FileSystemWatcher(root) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite };
        nextWatcher.Changed += (_, _) => dirty = true;
        nextWatcher.Created += (_, _) => dirty = true;
        nextWatcher.Deleted += (_, _) => dirty = true;
        nextWatcher.Renamed += (_, _) => dirty = true;
        nextWatcher.Error += (_, _) => dirty = true;
        try { nextWatcher.EnableRaisingEvents = true; }
        catch { nextWatcher.Dispose(); throw; }
        lock (gate)
        {
            Library = next;
            skins = nextSkins;
            watcher?.Dispose();
            watcher = nextWatcher;
            SweepPaused = false;
        }
        AllSkinsLoaded?.Invoke();
    }
    public static void RequestRefresh() => dirty = true;
    public static void Disconnect()
    {
        lock (gate) { SweepPaused = true; watcher?.Dispose(); watcher = null; Library = null; skins.Clear(); }
    }
    public static void Refresh()
    {
        var library = Library;
        if (library == null) return;
        var records = library.Load();
        lock (gate)
        {
            if (Library != library) return;
            var incoming = records.ToDictionary(r => SkinPaths.Identity(library.Root) + ":" + r.Id);
            foreach (var old in skins.ToArray())
                if (!incoming.ContainsKey(old.Key)) { skins.Remove(old.Key); SkinRemoved?.Invoke(old.Value); }
            foreach (var item in incoming)
            {
                if (!skins.TryGetValue(item.Key, out var skin)) AddSkin(new OsuSkin(library, item.Value));
                else if (skin.Record.Revision != item.Value.Revision || skin.Record.Problem != item.Value.Problem)
                {
                    skin.UpdateRecord(item.Value);
                    SkinModified?.Invoke(skin);
                }
            }
            dirty = false;
            lastRefresh = DateTime.UtcNow;
            if (library.Kind == OsuClientKind.Stable && SkinConflictDetected != null)
            {
                var conflict = skins.Values.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(g => g.Any(s => s.Hidden) && g.Any(s => !s.Hidden));
                if (conflict != null)
                {
                    SweepPaused = true;
                    SkinConflictDetected.Invoke(conflict.First(s => !s.Hidden), conflict.First(s => s.Hidden));
                }
            }
        }
    }
    public static void AddSkin(OsuSkin skin)
    {
        lock (gate) { if (skins.TryAdd(skin.Identity, skin)) SkinAdded?.Invoke(skin); }
    }
    public static void InvokeSkinModified(OsuSkin skin)
    {
        lock (gate)
        {
            foreach (var old in skins.Where(p => ReferenceEquals(p.Value, skin) && p.Key != skin.Identity).ToArray()) skins.Remove(old.Key);
            skin.ClearCache(); skins[skin.Identity] = skin; SkinModified?.Invoke(skin);
        }
        RequestRefresh();
    }
    public static void RemoveSkin(OsuSkin skin)
    {
        lock (gate) { if (skins.Remove(skin.Identity)) SkinRemoved?.Invoke(skin); }
    }
    public static void RequestSkinInfo(IEnumerable<OsuSkin> selected) => SkinInfoRequested?.Invoke(selected);
    public static void RequestSkinModify(IEnumerable<OsuSkin> selected)
    {
        if (selected.Any(s => !s.CanEdit)) { Settings.PushToast("Only complete legacy skins can be modified."); return; }
        SkinModifyRequested?.Invoke(selected);
    }
}
