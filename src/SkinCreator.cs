using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer;

public class SkinCreator : SkinMachine
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
            GD.Print($"About to copy option '{option.Name}' set to '{option.Skin?.Name ?? "null"}'");

            // User wants default skin elements to be used.
            if (option.Skin == null)
                continue;

            CopyOption(NewSkin, option);
            Progress += 100.0 / flattenedOptions.Count(o => o.Skin != null);

            CancellationToken.ThrowIfCancellationRequested();
        }

        string skinIniDestination = $"{NewSkin.Directory.FullName}/skin.ini";
        AddTask(() =>
        {
            GD.Print($"Writing to {skinIniDestination}");
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
        GD.Print($"Copying working folder to '{dirDestPath}'");

        if (!IsInSkinsFolder(dirDestPath))
            throw new InvalidOperationException("Destination path is not in the skins folder.");

        if (Directory.Exists(dirDestPath))
            Directory.Delete(dirDestPath, true);

        NewSkin.Directory.MoveTo(dirDestPath);

        OsuData.AddSkin(NewSkin);
    }

    private static bool IsInSkinsFolder(string path)
    {
        return Path.GetFullPath(path + "/..").TrimEnd('/').TrimEnd('\\').Equals(
            Path.GetFullPath(Settings.SkinsFolderPath).TrimEnd('/').TrimEnd('\\'),
            StringComparison.OrdinalIgnoreCase);
    }
}