using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Utils;

public class SkinModifierMachine : SkinMachine
{
    public const double UNCANCELLABLE_AFTER = 80.0;

    public IEnumerable<OsuSkin> SkinsToModify { get; set; }

    public bool SmoothTrail { get; set; }

    public bool Instafade { get; set; }

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
        Settings.Log($"Beginning skin modification for single skin '{workingSkin.Name}'");

        double progressInterval = UNCANCELLABLE_AFTER / SkinsToModify.Count() / flattenedOptions.Count(o => o.Value.Type != SkinOptionValueType.Unchanged);
        foreach (var option in flattenedOptions)
        {
            Settings.Log($"About to copy option '{option.Name}' set to {option.Value.Type}, skin '{option.Value.CustomSkin?.Name ?? "null"}'");

            // User wants this skin element to be unchanged.
            if (option.Value.Type == SkinOptionValueType.Unchanged || option.Value.CustomSkin == workingSkin)
                continue;

            CopyOption(workingSkin, option);
            Progress += progressInterval;

            CancellationToken.ThrowIfCancellationRequested();
        }

        if (SmoothTrail)
        {
            AddTask(() =>
            {
                if (File.Exists($"{workingSkin.Directory.FullName}/cursortrail.png"))
                {
                    MakeCursorTrailSmooth(workingSkin, $"{workingSkin.Directory.FullName}/cursortrail.png");
                    return;
                }
                if (File.Exists($"{workingSkin.Directory.FullName}/cursortrail@2x.png"))
                {
                    MakeCursorTrailSmooth(workingSkin, $"{workingSkin.Directory.FullName}/cursortrail@2x.png");
                    return;
                }

                Settings.Log($"No cursortrail image found for skin '{workingSkin.Name}', skipping smooth trail option.");
            });
        }

        string skinIniDestination = $"{workingSkin.Directory.FullName}/skin.ini";
        AddTask(() =>
        {
            Settings.Log($"Writing to {skinIniDestination}");
            File.WriteAllText(skinIniDestination, workingSkin.SkinIni.ToString());
        });

        Settings.Log($"Skin modification for '{workingSkin.Name}' has completed.");
    }

    private static void MakeCursorTrailSmooth(OsuSkin workingSkin, string cursorTrailPath)
    {
        Settings.Log($"Making cursor trail smooth for skin '{workingSkin.Name}'");

        using Image image = Image.Load(cursorTrailPath);
        int width = (int)(image.Width * 0.6);
        int height = (int)(image.Height * 0.6);

        image.Mutate(i => i.Resize(width, height));
        image.Save(cursorTrailPath);
    }

    protected override void CopyIniPropertyOption(OsuSkin workingSkin, SkinIniPropertyOption iniPropertyOption)
    {
        var property = iniPropertyOption.IncludeSkinIniProperty;

        // Remove the skin.ini properties to avoid remnants when using skin modifier.
        AddPriorityTask(() =>
        {
            Settings.Log($"Removing '{property.section}'.'{property.property}' to avoid remnants");
            workingSkin.SkinIni.Sections.LastOrDefault(s => s.Name == property.section)?.Remove(property.property);
        });

        base.CopyIniPropertyOption(workingSkin, iniPropertyOption);
    }

    protected override void CopyIniSectionOption(OsuSkin workingSkin, SkinIniSectionOption iniSectionOption)
    {
        OsuSkinIniSection section = workingSkin.SkinIni.Sections.Find(
            s => s.Name == iniSectionOption.SectionName && s.Contains(iniSectionOption.Property));

        // Remove the skin.ini section to avoid remnants when using skin modifier.
        AddPriorityTask(() =>
        {
            Settings.Log($"Removing skin.ini section '{iniSectionOption.SectionName}' where '{iniSectionOption.Property.Key}: {iniSectionOption.Property.Value}' to avoid remnants");
            workingSkin.SkinIni.Sections.Remove(section);
        });

        base.CopyIniSectionOption(workingSkin, iniSectionOption);
    }

    protected override void CopyFileOption(OsuSkin workingSkin, SkinFileOption fileOption)
    {
        // Remove old files to avoid remnants, so if there is no match the default skin will be used.
        AddPriorityTask(() =>
        {
            foreach (FileInfo file in workingSkin.Directory.GetFiles().Where(f => CheckIfFileAndOptionMatch(f, fileOption)).ToArray())
            {
                Settings.Log($"Removing '{file.FullName}' to avoid remnants");
                file.Delete();
            }
        });

        base.CopyFileOption(workingSkin, fileOption);
    }
}