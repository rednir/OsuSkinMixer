using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;
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

        if (Instafade)
        {
            AddTask(() => MakeCirclesInstafade(workingSkin));
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

    private static void MakeCirclesInstafade(OsuSkin workingSkin)
    {
        // With help from https://skinship.xyz/guides/insta_fade_hc.html
        Settings.Log($"Making circles instafade for skin '{workingSkin.Name}'");

        string skinDirectory = workingSkin.Directory.FullName;
        string hitcirclePath = $"{skinDirectory}/hitcircle.png";
        string hitcircleoverlayPath = $"{skinDirectory}/hitcircleoverlay.png";

        Image<Rgba32> hitcircle;
        Image hitcircleoverlay;

        try
        {
            hitcircle = Image.Load<Rgba32>(hitcirclePath);
            hitcircleoverlay = Image.Load(hitcircleoverlayPath);
        }
        catch
        {
            // TOOD: use default skin.
            throw;
        }

        hitcircleoverlay.Mutate(i => i.Resize((int)(hitcircleoverlay.Width * 1.25), (int)(hitcircleoverlay.Height * 1.25)));
        hitcircle.Mutate(i => i.Resize((int)(hitcircle.Width * 1.1), (int)(hitcircle.Height * 1.1)));

        Godot.Color[] comboColors = workingSkin.ComboColors;

        for (int i = 0; i <= 9; i++)
        {
            string defaultXPath = $"{skinDirectory}/default-{i}.png";
            Image defaultX = Image.Load(defaultXPath);

            Image<Rgba32> newDefaultX = hitcircle.Clone();

            if (i <= comboColors.Length)
            {
                // Modulate the image with the combo color.
                Godot.Color color = comboColors[(i + 1) % comboColors.Length];
                for (int x = 0; x < newDefaultX.Width; x++)
                {
                    for (int y = 0; y < newDefaultX.Height; y++)
                    {
                        Rgba32 oldColor = newDefaultX[x, y];
                        byte red = (byte)(256.0f * (oldColor.R / 256.0f) * color.R);
                        byte green = (byte)(256.0f * (oldColor.G / 256.0f) * color.G);
                        byte blue = (byte)(256.0f * (oldColor.B / 256.0f) * color.B);

                        newDefaultX[x, y] = Color.FromRgba(red, green, blue, oldColor.A);
                    }
                }
            }

            newDefaultX.Mutate(img =>
            {
                img.DrawImage(hitcircleoverlay, new Point(
                    x: (int)((hitcircle.Width * 0.5) - (hitcircleoverlay.Width * 0.5)),
                    y: (int)((hitcircle.Height * 0.5) - (hitcircleoverlay.Height * 0.5))
                ), 1);
                img.DrawImage(defaultX, new Point(
                    x: (int)((hitcircle.Width * 0.5) - (defaultX.Width * 0.5)),
                    y: (int)((hitcircle.Height * 0.5) - (defaultX.Height * 0.5))
                ), 1);
            });

            if (i == 0)
                newDefaultX.Mutate(i => i.Opacity(0));

            newDefaultX.Save(defaultXPath);
        }

        hitcircle.Mutate(i => i.Opacity(0));
        hitcircleoverlay.Mutate(i => i.Opacity(0));

        hitcircle.Save(hitcirclePath);
        hitcircleoverlay.Save(hitcircleoverlayPath);
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