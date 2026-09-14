# Stable and lazer storage

## Current behaviour

Select the osu! **data root**, not its `Skins` or `files` directory. The app detects `client.realm` on each connection/startup; otherwise it uses `Skins` and `HiddenSkins`. No client-type setting is persisted.

Both the mixer and modifier now install changes directly into the selected library. This supersedes the original plan's default OSK-import route and optional direct-install checkbox, following the requested behaviour change.

- Creating a mix with an existing name overwrites that skin and opens its skin-info page. Lazer replacement preserves the existing GUID. Stable retains its visible/hidden-folder overwrite behaviour. Overwrite undo restores the previous content rather than merely deleting the replacement.
- If multiple active lazer skins have exactly the requested name, creation stops without picking an arbitrary target. Rename one or modify a specific skin first.
- Lazer direct operations require osu! closed, a verified backup, and explicit confirmation for **each** operation, including undo. Do not start osu! while a confirmation or write is in progress.
- Only complete, legacy-format user skins can be changed. Protected built-ins are excluded. Non-legacy skins and damaged mappings are not silently treated as writable legacy skins.
- Lazer Hide and Open Folder remain visible but disabled. Export an OSK for external editing. Stable exports remain in `<osu root>/Exports/<name>.osk`; lazer exports use an app-data export subdirectory containing `<name>.osk`. The unique directory, not the filename, distinguishes repeated exports.
- Opening an OSK uses the OS file association. This is an import request, **not** confirmation that a particular selected library was changed. Normal mixing/modification does not depend on OSK import. OSK import alone does not reliably identify an existing skin to replace.

## Architecture

`storage/OsuSkinMixer.Storage.csproj` is a Godot-independent .NET 10 project using Realm 20.1.0. `ISkinLibrary` owns discovery, materialisation, installation, replacement, duplication, renaming, export, deletion, and restore. `SkinRecord` is detached metadata and virtual filename-to-physical-file mappings. No managed Realm object crosses this boundary.

`OsuSkin` is the UI-facing handle. Its identity is the owning library root plus a canonical stable directory identity or lazer GUID, never the display name. Existing image-processing algorithms receive unique, app-owned `SkinWorkspace` directories. A run snapshots each source once; modifiers edit copies and commit completed workspaces. Preview images/audio/animations resolve virtual files; extensionless lazer image blobs are decoded from bytes using the virtual extension. Only requested asynchronous preview images are copied to a preview workspace.

`OsuData` is the catalogue facade. It coalesces filesystem notifications, periodically refreshes as a fallback, and requests refresh on application focus and internal operations. Existing handles are updated by identity. UI colour overrides and history use identity rather than names.

Relevant entry points:

- `storage/SkinLibrary.cs`: contracts, path validation, workspaces, snapshots.
- `storage/StableSkinLibrary.cs`: folder operations, overwrite/recovery.
- `storage/LazerSkinLibrary.cs`: schema checks, backups, guards, content-addressed files and transactions.
- `storage/LazerModels.cs`: mapped `Skin`, `File`, and embedded `RealmNamedFileUsage` models.
- `src/Statics/LibraryActions.cs`: confirmation dialogs and export/import UI helpers.
- `src/Services/SkinMixerMachine.cs`, `SkinModifierMachine.cs`: workspace processing and commit.

## Database invariants and recovery

The known schema versions are 51 and 52. Other versions can be read if the skin subset is compatible. Direct writes to an unknown version require a one-operation advanced warning; no persistent override exists. Required-field/type incompatibilities block access. Additional persisted properties on a mapped table allow compatible reads but block writes: the adapter must be updated, not migrate away unfamiliar fields. Additional unrelated tables are retained. The app never changes the schema version, runs a migration, or uses a delete-if-migration-needed option.

Writes are serialised in-process. Known osu! process names and exclusive database-file access are checked, reads are disposed, and a full Realm `WriteCopy` backup is reopened read-only before confirmation. The most recent five verified backups per root are retained under `user://realm-backups/<root hash>/`. Backups have owner-only permissions on Unix. These are **database backups**, not backups of the entire file store. They contain private osu! data; do not attach them to bug reports.

Process-name detection and `FileShare.None` are advisory, not a cross-process guarantee that every custom osu! executable or launch race can be detected. Explicit closed-client confirmation is therefore required. The application does not claim live concurrent-write support.

Content hashes are raw lowercase SHA-256 hex, without a `sha256:` prefix. Files resolve to `files/<first character>/<first two characters>/<hash>`. New bytes are staged, verified, and moved into place. Existing content is verified, not overwritten. `File` rows are committed before skin mappings; interrupted skin commits leave registered, unreferenced content for osu!'s cleanup. Skin aggregate hashes concatenate final INI/JSON bytes in filename order, matching the upstream importer's ordering convention. Credits must be written before hashing/installation.

No operation deletes lazer blobs, which may be shared by skins or other game data. Delete sets `DeletePending`. Undo requires the original row and original blobs to remain present; osu!'s startup cleanup can invalidate undo. Modification/overwrite restore uses a manifest and rejects later conflicting edits. Whole-database backups are never automatically restored, since that would also roll back unrelated game data.

Stable replacements are staged alongside their destination, with recoverable copies under `user://skin-recovery/`. Deleted stable skins are retained there as well. History is session-only; recovery files persist after exit. Temporary workspaces use unique per-session directories and are removed on normal application exit. Abrupt process termination may leave temporary workspaces for manual cleanup.

## Verification

Run the storage suite (no live osu! data required):

```sh
dotnet test tests/OsuSkinMixer.Storage.Tests.csproj
dotnet build OsuSkinMixer.csproj
dotnet build OsuSkinMixer.csproj -c ExportRelease
```

Run the actual Godot processing/navigation smoke test with Godot 4.7.2 .NET:

```sh
godot --headless --path . --log-file /tmp/osm-smoke.log tests/Smoke.tscn
```

This scene bypasses normal application startup/settings and uses temporary stable and synthetic lazer libraries. It checks metadata, actual custom texture/audio bytes, animation frames, mixing, same-name overwrite, modifier commits, undo, credits, export, cancellation, and the mixer UI's skin-info navigation signal.

Local verification: Debug and ExportRelease builds succeeded; 27 storage/integration tests passed with the optional copied-database fixture enabled. The headless workflow passed for both clients, including actual mixer navigation. Godot emitted macOS certificate-store and shutdown resource-leak diagnostics during headless scene teardown; a zero exit code here is not a clean graphical release certification. Cross-platform packaging and real-game reopen checks remain on the release checklist below.

An optional additional test copies a supplied database fixture into its own private temporary directory, inserts a synthetic skin, and verifies unrelated table counts:

```sh
OSM_REALM_FIXTURE=/path/to/disposable-client-copy.realm dotnet test tests/OsuSkinMixer.Storage.Tests.csproj
```

Never run game-level write experiments against a primary installation. For a manual release test, make a consistent copy of both `client.realm` and `files` while osu! is closed and use an isolated osu! data directory.

## Release checklist

- [ ] Windows x64, Linux x64, macOS Intel/Apple Silicon: launch the packaged app and verify Realm native-library loading. Local macOS development builds do not substitute for these checks.
- [ ] In an isolated installation, create a mix, reopen osu!, select it, and verify image/audio/INI behaviour.
- [ ] Repeat with same-name overwrite, modifier operations, rename, duplicate, delete, and undo while closed; reopen osu! after each completed operation.
- [ ] Start osu! before a write and verify refusal. Cancel the closed-client confirmation and verify no library changes.
- [ ] Verify cleanup-invalidated undo reports a clear error and does not resurrect missing content.
- [ ] Check OSK file associations/manual import on all three platforms; the displayed name must not contain a generated identifier.
- [ ] Visually check narrow-window layouts, loading/confirmation dialogs, read-only explanations and skin-info navigation.

## Upstream references

The adapter was checked against osu!'s [skin model](https://github.com/ppy/osu/blob/master/osu.Game/Skinning/SkinInfo.cs), [skin importer](https://github.com/ppy/osu/blob/master/osu.Game/Skinning/SkinImporter.cs), [archive importer](https://github.com/ppy/osu/blob/master/osu.Game/Database/RealmArchiveModelImporter.cs), [file store](https://github.com/ppy/osu/blob/master/osu.Game/Database/RealmFileStore.cs), and [Realm access/schema version](https://github.com/ppy/osu/blob/master/osu.Game/Database/RealmAccess.cs). These are internal game storage details, not a stable external editing API; repeat compatibility checks when upgrading the dependency or supporting a new schema.
