namespace OsuSkinMixer.Utils;

using System.IO;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

/// <summary>Provides methods to create and import a new skin from a list of <see cref="SkinOption"/>.</summary>
public class SkinMixerMachine : SkinMachine
{
    private OsuSkinMixer.Storage.SkinWorkspace workingWorkspace;

    public OsuSkin NewSkin { get; private set; }

    public void SetNewSkin(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Skin name cannot be empty.");

        workingWorkspace?.Dispose();
        workingWorkspace = new OsuSkinMixer.Storage.SkinWorkspace();
        NewSkin = new OsuSkin(name, new DirectoryInfo(workingWorkspace.DirectoryPath));
    }

    protected override void PopulateTasks()
    {
        if (NewSkin == null)
            throw new InvalidOperationException("New skin not set.");

        var flattenedOptions = FlattenedBottomLevelOptions;
        foreach (var option in flattenedOptions)
        {
            Log($"About to copy option '{option.Name}' set to '{option.Value.CustomSkin?.Name ?? "null"}'");

            if (option.Value.Type == SkinOptionValueType.DefaultSkin)
                continue;

            CopyOption(NewSkin, option);
            Progress += 40.0 / flattenedOptions.Count(o => o.Value.Type != SkinOptionValueType.DefaultSkin);

            CancellationToken.ThrowIfCancellationRequested();
        }

        string skinIniDestination = $"{NewSkin.Directory.FullName}/skin.ini";
        AddTask(() =>
        {
            Log($"Writing to {skinIniDestination}");
            File.WriteAllText(skinIniDestination, NewSkin.SkinIni.ToString());
        });

        // There might be skin elements from a failed attempt still in the working directory.
        // If so, delete them before peforming any tasks.
        AddPriorityTask(() =>
        {
            foreach (var file in NewSkin.Directory.EnumerateFiles())
                file.Delete();
        });
    }

    public bool Installed { get; private set; }
    protected override void CleanupAfterRun() => workingWorkspace?.Dispose();
    public bool Reused { get; private set; }
    public Action UndoInstallation { get; private set; }

    protected override void PostRun()
    {
        StatusChanged?.Invoke("Installing skin...");
        GenerateCreditsFile(NewSkin);
        CancellationToken.ThrowIfCancellationRequested();
        var library = OsuData.Library;
        var result = library.Install(workingWorkspace, overwriteExisting: true);
        NewSkin = OsuData.ApplyInstall(result);
        Reused = result.Reused;
        Installed = true;
        if (Reused) Settings.PushToast("An identical skin already exists. Reused the existing skin.");
        else UndoInstallation = () => { library.UndoInstall(result); OsuData.Refresh(); };
    }
}
