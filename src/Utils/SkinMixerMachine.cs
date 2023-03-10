using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Utils;

/// <summary>Provides methods to create and import a new skin from a list of <see cref="SkinOption"/>.</summary>
public class SkinMixerMachine : SkinMachine
{
    private const string WORKING_DIR_NAME = ".osu-skin-mixer_working-skin";

    public string NewSkinName { get; set; }

    public OsuSkin NewSkin { get; set; }

    protected override void PopulateTasks()
    {
        if (string.IsNullOrWhiteSpace(NewSkinName))
            throw new InvalidOperationException("Skin name cannot be empty.");

        NewSkin = new OsuSkin(NewSkinName, Directory.CreateDirectory($"{Path.GetTempPath()}/{WORKING_DIR_NAME}"));

        var flattenedOptions = FlattenedBottomLevelOptions;
        foreach (var option in flattenedOptions)
        {
            Settings.Log($"About to copy option '{option.Name}' set to '{option.Value.CustomSkin?.Name ?? "null"}'");

            if (option.Value.Type == SkinOptionValueType.DefaultSkin)
                continue;

            CopyOption(NewSkin, option);
            Progress += 100.0 / flattenedOptions.Count(o => o.Value.Type != SkinOptionValueType.DefaultSkin);

            CancellationToken.ThrowIfCancellationRequested();
        }

        string skinIniDestination = $"{NewSkin.Directory.FullName}/skin.ini";
        AddTask(() =>
        {
            Settings.Log($"Writing to {skinIniDestination}");
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

    protected override void PostRun()
    {
        string dirDestPath = $"{Settings.SkinsFolderPath}/{NewSkinName}";
        Settings.Log($"Copying working folder to '{dirDestPath}'");

        if (!IsInSkinsFolder(dirDestPath))
            throw new InvalidOperationException("Destination path is not in the skins folder.");

        // Also replace the skin in the hidden skins folder to avoid duplicate names.
        if (Directory.Exists($"{Settings.HiddenSkinsFolderPath}/{NewSkinName}"))
            Directory.Delete($"{Settings.HiddenSkinsFolderPath}/{NewSkinName}", true);

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

        OsuData.AddSkin(NewSkin);
    }

    private static bool IsInSkinsFolder(string path)
    {
        return Path.GetFullPath(path + "/..").TrimEnd('/').TrimEnd('\\').Equals(
            Path.GetFullPath(Settings.SkinsFolderPath).TrimEnd('/').TrimEnd('\\'),
            StringComparison.OrdinalIgnoreCase);
    }
}