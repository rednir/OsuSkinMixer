namespace OsuSkinMixer.Tests;

using System.IO;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Storage;
using OsuSkinMixer.Utils;
using Realms;

/// <summary>Explicit headless test scene. Never loaded by the application.</summary>
public partial class StorageSmoke : Node
{
    public override async void _Ready()
    {
        var root = Path.Combine(Path.GetTempPath(), "osm-godot-smoke-" + Guid.NewGuid().ToString("N"));
        try
        {
            var warning = GD.Load<PackedScene>("res://src/Components/Popup/LazerWarningPopup.tscn").Instantiate<Components.LazerWarningPopup>();
            AddChild(warning);
            var warningAccepted = warning.ConfirmAsync();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var proceed = warning.GetNode<Button>("%ProceedButton");
            Check(warning.GetNode<CanvasLayer>("Popup/CanvasLayer").Visible && proceed.Disabled,
                "lazer warning starts unacknowledged");
            warning.GetNode<CheckBox>("%UnderstandCheckBox").ButtonPressed = true;
            Check(!proceed.Disabled, "lazer warning acknowledgement enables proceed");
            proceed.EmitSignal(Button.SignalName.Pressed);
            Check(await warningAccepted, "lazer warning proceed result");
            var warningCancelled = warning.ConfirmAsync();
            warning.GetNode<Button>("%CancelButton").EmitSignal(Button.SignalName.Pressed);
            Check(!await warningCancelled, "lazer warning cancel result");
            warning.QueueFree();

            foreach (bool lazer in new[] { false, true })
            {
                var folder = Path.Combine(root, lazer ? "lazer" : "stable");
                Directory.CreateDirectory(Path.Combine(folder, "Skins"));
                Settings.Content = new Settings.SettingsContent { OsuFolder = folder };
                ISkinLibrary library;
                if (lazer)
                {
                    using (Realm.GetInstance(LazerSkinLibrary.Configuration(Path.Combine(folder, "client.realm"), 51, false))) { }
                    library = new LazerSkinLibrary(folder, Path.Combine(root, "backups"));
                }
                else library = new StableSkinLibrary(folder, Path.Combine(root, "recovery"));
                using var source = new SkinWorkspace();
                // Repeated General/Name values occur in real legacy skins and must not override copy names.
                File.WriteAllText(Path.Combine(source.DirectoryPath, "SKIN.INI"), "[General]\nName: Source\nAuthor: Smoke\n[Colours]\nCombo1: 255,0,0\n[Fonts]\n[CatchTheBeat]\n[General]\nName: Source\n");
                using (var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8))
                {
                    image.Fill(Colors.Red);
                    image.SavePng(Path.Combine(source.DirectoryPath, "hitcircle.png"));
                    image.SavePng(Path.Combine(source.DirectoryPath, "hitcircle-0.png"));
                    image.SavePng(Path.Combine(source.DirectoryPath, "hitcircle-1.png"));
                }
                using (var wav = new BinaryWriter(File.Create(Path.Combine(source.DirectoryPath, "menuhit.wav"))))
                {
                    wav.Write("RIFF"u8); wav.Write(38); wav.Write("WAVEfmt "u8); wav.Write(16);
                    wav.Write((short)1); wav.Write((short)1); wav.Write(44100); wav.Write(88200);
                    wav.Write((short)2); wav.Write((short)16); wav.Write("data"u8); wav.Write(2); wav.Write((short)0);
                }
                var sourceRecord = await Task.Run(() => library.Install(source).Skin);
                OsuData.Connect(library);
                var skin = OsuData.Skins.Single();

                var settingsPopup = GD.Load<PackedScene>("res://src/Components/Popup/SettingsPopup.tscn").Instantiate<Components.SettingsPopup>();
                AddChild(settingsPopup);
                settingsPopup.In();
                Check(settingsPopup.GetNode<VBoxContainer>("%LazerBackupContainer").Visible == lazer,
                    "backup settings follow connected client");
                if (lazer)
                    Check(settingsPopup.GetNode<Label>("%LazerBackupStatus").Text.Contains("verified database backup"),
                        "backup settings report verified backups");
                settingsPopup.QueueFree();

                var managePopup = GD.Load<PackedScene>("res://src/Components/Popup/ManageSkinPopup.tscn").Instantiate<Components.ManageSkinPopup>();
                AddChild(managePopup);
                managePopup.SetSkin(skin);
                managePopup.In();
                Check(managePopup.GetNode<Button>("%RenameButton").Visible, "scene rename button is wired");
                Check(managePopup.GetNode<Button>("%OpenFolderButton").Visible != lazer, "open-folder action follows client capabilities");
                Check(managePopup.GetNode<Button>("%HideButton").Visible != lazer, "hide action follows client capabilities");
                managePopup.QueueFree();

                var manager = GD.Load<PackedScene>("res://src/StackScenes/SkinManager.tscn").Instantiate<StackScenes.SkinManager>();
                AddChild(manager);
                var sortChips = manager.GetNode<SkinSortChipsContainer>("%SkinSortChipsContainer");
                Check(sortChips.GetNode<Button>("Hidden").Visible != lazer, "hidden sort follows client capabilities");
                manager.QueueFree();

                Check(skin.SkinIni.TryGetPropertyValue("General", "Author") == "Smoke", "INI metadata");
                Check(skin.GetTexture("hitcircle").GetImage().GetPixel(0, 0).IsEqualApprox(Colors.Red), "custom texture pixels (not fallback)");
                Check(skin.GetAudioStream("menuhit") is AudioStreamWav sound && sound.Data.Length == 2, "custom audio from virtual file (not fallback)");
                using (var frames = new SpriteFrames())
                {
                    skin.AddSpriteFramesAnimation(frames, "hitcircle", false);
                    Check(frames.GetFrameCount("hitcircle") == 2, "animation virtual frames");
                    Check(frames.GetFrameTexture("hitcircle", 0) != null, "custom animation texture decoded");
                }
                var mixer = new SkinMixerMachine
                {
                    SkinOptions = [new SkinFileOption("hitcircle", false) { Value = new SkinOptionValue(skin) }],
                };
                mixer.SetNewSkin("Mixed");
                await Task.Run(() => mixer.Run(CancellationToken.None));
                var mixed = mixer.NewSkin;
                Check(mixer.Installed, "mix installation");
                Check(mixed.Record.Files.ContainsKey("credits.ini"), "credits persisted");
                var renamePopup = GD.Load<PackedScene>("res://src/Components/Popup/ManageSkinPopup.tscn").Instantiate<Components.ManageSkinPopup>();
                AddChild(renamePopup);
                renamePopup.SetSkin(skin);
                renamePopup.OnRenameButtonPressed();
                var renameDialog = renamePopup.GetNode<Components.SkinNamePopup>("%SkinNamePopup");
                renameDialog.LineEditText = mixed.Name;
                renameDialog.In();
                Check(renameDialog.GetNode<Button>("%ConfirmButton").Disabled, "rename rejects an existing skin name");
                renamePopup.QueueFree();
                var hash = SkinPaths.Hash(mixed.FindFile("hitcircle.png"));
                Check(hash == SkinPaths.Hash(source.Files["hitcircle.png"]), "mixed content");
                var overwriteMixer = new SkinMixerMachine
                {
                    SkinOptions = [new SkinFileOption("hitcircle", false) { Value = new SkinOptionValue(SkinOptionValueType.Blank) }],
                };
                overwriteMixer.SetNewSkin("Mixed");
                await Task.Run(() => overwriteMixer.Run(CancellationToken.None));
                Check(overwriteMixer.NewSkin.Record.Id == mixed.Record.Id, "same-name mix preserves identity");
                Check(library.Load().Count == 2, "same-name mix replaces rather than duplicates");
                await Task.Run(overwriteMixer.UndoInstallation);
                mixed = OsuData.Skins.Single(s => s.Name == "Mixed");
                Check(SkinPaths.Hash(mixed.FindFile("hitcircle.png")) == hash, "overwrite undo restores prior mix");
                var modifier = new SkinModifierMachine
                {
                    SkinsToModify = [mixed],
                    SkinComboColourOverrides = new(),
                    SkinCursorColourOverrideImageDirs = new(),
                    SkinOptions = [new SkinFileOption("hitcircle", false) { Value = new SkinOptionValue(SkinOptionValueType.Blank) }],
                };
                await Task.Run(() => modifier.Run(CancellationToken.None));
                Check(SkinPaths.Hash(mixed.FindFile("hitcircle.png")) != hash, "workspace modification");
                Settings.Content.Operations.Last().UndoOperation();
                while (Operation.IsBusy) await Task.Delay(20);
                Check(SkinPaths.Hash(mixed.FindFile("hitcircle.png")) == hash, "undo restores original bytes");
                Check(SkinPaths.Hash(skin.FindFile("hitcircle.png")) == hash, "source remained unchanged");
                var export = Path.Combine(folder, "result.osk"); mixed.Export(export);
                Check(File.Exists(export), "OSK export");
                var cancelled = new SkinMixerMachine { SkinOptions = [] };
                cancelled.SetNewSkin("Cancelled");
                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();
                    try { await Task.Run(() => cancelled.Run(cancellation.Token)); throw new Exception("Cancellation ignored"); }
                    catch (OperationCanceledException) { }
                }
                Check(!library.Load().Any(s => s.Name == "Cancelled"), "cancelled mix has no disk effects");

                // Exercise the actual mixer UI callback, including post-creation navigation.
                var screen = GD.Load<PackedScene>("res://src/StackScenes/SkinMixer.tscn").Instantiate<StackScenes.SkinMixer>();
                var navigated = new TaskCompletionSource<StackScenes.StackScene>();
                screen.ScenePushed += scene => navigated.TrySetResult(scene);
                AddChild(screen);
                var optionsSelector = screen.GetNode<Components.SkinOptionsSelector>("%SkinOptionsSelector");
                Check(optionsSelector.GetNode<PanelContainer>("%LazerMenuWarningContainer").Visible == lazer, "mixer lazer warning follows connected client");
                typeof(StackScenes.SkinMixer).GetMethod("RunSkinCreator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(screen, ["UI created"]);
                var info = await navigated.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Check(info is StackScenes.SkinInfo skinInfo && skinInfo.Skins.Single().Name == "UI created", "creation navigates to skin info");
                info.Free(); screen.QueueFree();

                // Exercise ordinary duplication from the management popup and verify its callback target.
                var duplicatePopup = GD.Load<PackedScene>("res://src/Components/Popup/ManageSkinPopup.tscn").Instantiate<Components.ManageSkinPopup>();
                var duplicated = new TaskCompletionSource<OsuSkin>();
                duplicatePopup.SkinInfoRequested = skins => duplicated.TrySetResult(skins.Single());
                AddChild(duplicatePopup);
                duplicatePopup.SetSkin(skin);
                duplicatePopup.In();
                duplicatePopup.OnDuplicateButtonPressed();
                var duplicateNamePopup = duplicatePopup.GetNode<Components.SkinNamePopup>("%SkinNamePopup");
                duplicateNamePopup.LineEditText = "Managed copy";
                duplicateNamePopup.In();
                duplicateNamePopup.GetNode<Button>("%ConfirmButton").EmitSignal(Button.SignalName.Pressed);
                var managedCopy = await duplicated.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Check(managedCopy.Name == "Managed copy" && managedCopy.Identity != skin.Identity,
                    "ordinary duplicate callback targets the newly duplicated skin");
                duplicatePopup.QueueFree();

                // Exercise the modifier's "make a copy first" flow through its real popup callback.
                async Task<OsuSkin> CopyForModification(string name)
                {
                    var modifierSelect = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierSkinSelect.tscn").Instantiate<StackScenes.SkinModifierSkinSelect>();
                    var modifierNavigated = new TaskCompletionSource<StackScenes.StackScene>();
                    modifierSelect.ScenePushed += scene => modifierNavigated.TrySetResult(scene);
                    AddChild(modifierSelect);
                    typeof(StackScenes.SkinModifierSkinSelect).GetMethod("AddSkinComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(modifierSelect, [skin]);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    modifierSelect.GetNode<CheckBox>("%MakeCopyCheckBox").ButtonPressed = true;
                    modifierSelect.GetNode<Button>("%ContinueButton").EmitSignal(Button.SignalName.Pressed);
                    var namePopup = modifierSelect.GetNode<Components.ManageSkinPopup>("%ManageSkinPopup").GetNode<Components.SkinNamePopup>("%SkinNamePopup");
                    namePopup.LineEditText = name;
                    namePopup.In();
                    if (OsuData.Skins.Any(existing => existing.Name == name))
                    {
                        Check(namePopup.GetNode<Label>("%WarningLabel").Text == "Skin with this name already exists and will be replaced.",
                            "modifier copy describes same-name overwrite");
                    }
                    namePopup.GetNode<Button>("%ConfirmButton").EmitSignal(Button.SignalName.Pressed);
                    var next = await modifierNavigated.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    var target = (next as StackScenes.SkinModifierModificationSelect)?.SkinsToModify.Single();
                    Check(target != null, "modifier copy navigates with a target skin");
                    next.Free(); modifierSelect.QueueFree();
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    return target;
                }

                var newlyNamedCopy = await CopyForModification("Modifier copy");
                Check(newlyNamedCopy.Name == "Modifier copy" && newlyNamedCopy.Identity != skin.Identity,
                    "modifier new-name copy targets the copied skin");
                var overwrittenCopy = await CopyForModification(mixed.Name);
                Check(overwrittenCopy.Record.Id == mixed.Record.Id && overwrittenCopy.Identity != skin.Identity,
                    "modifier same-name copy overwrites and targets the copied skin");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                OsuData.Disconnect();
                GD.Print($"SMOKE PASS: {(lazer ? "lazer" : "stable")} metadata, preview, mixer, modifier, undo, credits, export");
            }
            GetTree().Quit(0);
        }
        catch (Exception e) { GD.PrintErr("SMOKE FAILED: " + e); GetTree().Quit(1); }
        finally
        {
            OsuData.Disconnect();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
    private static void Check(bool condition, string name) { if (!condition) throw new Exception("Failed: " + name); }
}
