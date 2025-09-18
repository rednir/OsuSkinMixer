namespace OsuSkinMixer.Utils;

using System.IO;
using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Statics;

/// <summary>Provides methods to create and import a new skin from a list of <see cref="SkinOption"/>.</summary>
public class SkinMixerMachine : SkinMachine
{
    private const string WORKING_DIR_NAME = ".osu-skin-mixer_working-skin";

    public OsuSkinStable NewSkin { get; private set; }

    public void SetNewSkin(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Skin name cannot be empty.");

        NewSkin = new OsuSkinStable(name, Directory.CreateDirectory($"{Path.GetTempPath()}/{WORKING_DIR_NAME}"));
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
            Progress += 100.0 / flattenedOptions.Count(o => o.Value.Type != SkinOptionValueType.DefaultSkin);

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

    protected override void PostRunStable()
    {
        string dirDestPath = $"{Settings.SkinsFolderPath}/{NewSkin.Name}";
        Log($"Copying working folder to '{dirDestPath}'");

        if (!IsInSkinsFolder(dirDestPath))
            throw new InvalidOperationException("Destination path is not in the skins folder.");

        // Also replace the skin in the hidden skins folder to avoid duplicate names.
        if (Directory.Exists($"{Settings.HiddenSkinsFolderPath}/{NewSkin.Name}"))
            Directory.Delete($"{Settings.HiddenSkinsFolderPath}/{NewSkin.Name}", true);

        if (Directory.Exists(dirDestPath))
            Directory.Delete(dirDestPath, true);

        try
        {
            NewSkin.Directory.MoveTo(dirDestPath);
        }
        catch (IOException e)
        {
            GD.PushWarning($"Exception thrown, probably because we are trying to move across different volumes or devices. Falling back to copy method.\n{e.Message}");
            DirectoryInfo copiedDir = NewSkin.Directory.CopyDirectory(dirDestPath);
            NewSkin.Directory.Delete(true);
            NewSkin.Directory = copiedDir;
        }

        try
        {
            GenerateCreditsFile(NewSkin);
        }
        catch (Exception e)
        {
            Settings.PushException(new InvalidOperationException($"Failed to generate credits file for {NewSkin.Name}. The skin was still created successfully, don't worry.", e));
        }

        OsuData.AddSkin(NewSkin);
    }

    protected override void PostRunLazer()
    {

    }

    private static bool IsInSkinsFolder(string path)
    {
        return Path.GetFullPath(path + "/..").TrimEnd('/').TrimEnd('\\').Equals(
            Path.GetFullPath(Settings.SkinsFolderPath).TrimEnd('/').TrimEnd('\\'),
            StringComparison.OrdinalIgnoreCase);
    }
}