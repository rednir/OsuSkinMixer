using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Utils;

public class SkinModifierMachine : SkinMachine
{
    public const double UNCANCELLABLE_AFTER = 80.0;

    public IEnumerable<OsuSkin> SkinsToModify { get; set; }

    protected override void PopulateTasks()
    {
        var flattenedOptions = FlattenedBottomLevelOptions;
        foreach (OsuSkin skin in SkinsToModify)
        {
            ModifySingleSkin(skin, flattenedOptions);
            CancellationToken.ThrowIfCancellationRequested();
        }
    }

    protected override void PostRun()
    {
        foreach (OsuSkin skin in SkinsToModify)
            OsuData.InvokeSkinModified(skin);
    }

    private void ModifySingleSkin(OsuSkin workingSkin, IEnumerable<SkinOption> flattenedOptions)
    {
        GD.Print($"Beginning skin modification for single skin '{workingSkin.Name}'");

        double progressInterval = UNCANCELLABLE_AFTER / SkinsToModify.Count() / flattenedOptions.Count(o => o.Value.Type != SkinOptionValueType.Unchanged);
        foreach (var option in flattenedOptions)
        {
            GD.Print($"About to copy option '{option.Name}' set to {option.Value.Type}, skin '{option.Value.CustomSkin?.Name ?? "null"}'");

            // User wants this skin element to be unchanged.
            if (option.Value.Type == SkinOptionValueType.Unchanged || option.Value.CustomSkin == workingSkin)
                continue;

            CopyOption(workingSkin, option);
            Progress += progressInterval;

            CancellationToken.ThrowIfCancellationRequested();
        }

        string skinIniDestination = $"{workingSkin.Directory.FullName}/skin.ini";
        AddTask(() =>
        {
            GD.Print($"Writing to {skinIniDestination}");
            File.WriteAllText(skinIniDestination, workingSkin.SkinIni.ToString());
        });

        GD.Print($"Skin modification for '{workingSkin.Name}' has completed.");
    }

    protected override void CopyIniPropertyOption(OsuSkin workingSkin, SkinIniPropertyOption iniPropertyOption)
    {
        var property = iniPropertyOption.IncludeSkinIniProperty;

        // Remove the skin.ini properties to avoid remnants when using skin modifier.
        AddPriorityTask(() =>
        {
            GD.Print($"Removing '{property.section}'.'{property.property}' to avoid remnants");
            workingSkin.SkinIni.Sections.LastOrDefault(s => s.Name == property.section)?.Remove(property.property);
        });

        base.CopyIniPropertyOption(workingSkin, iniPropertyOption);
    }

    protected override void CopyFileOption(OsuSkin workingSkin, SkinFileOption fileOption)
    {
        // Remove old files to avoid remnants, so if there is no match the default skin will be used.
        AddPriorityTask(() =>
        {
            foreach (FileInfo file in workingSkin.Directory.GetFiles().Where(f => CheckIfFileAndOptionMatch(f, fileOption)).ToArray())
            {
                GD.Print($"Removing '{file.FullName}' to avoid remnants");
                file.Delete();
            }
        });

        base.CopyFileOption(workingSkin, fileOption);
    }
}