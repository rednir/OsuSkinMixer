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
                File.WriteAllText(Path.Combine(source.DirectoryPath, "SKIN.INI"), "[General]\nName: Source\nAuthor: Smoke\n[Colours]\nCombo1: 255,0,0\n[Fonts]\n[CatchTheBeat]\n");
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
                typeof(StackScenes.SkinMixer).GetMethod("RunSkinCreator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(screen, ["UI created"]);
                var info = await navigated.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Check(info is StackScenes.SkinInfo skinInfo && skinInfo.Skins.Single().Name == "UI created", "creation navigates to skin info");
                info.Free(); screen.QueueFree();
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
