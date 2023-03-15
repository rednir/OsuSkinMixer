using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Utils;

/// <summary>Provides methods to modify one or more skins based on a list of <see cref="SkinOption"/> and extra flags.</summary>
public class SkinModifierMachine : SkinMachine
{
    public const double UNCANCELLABLE_AFTER = 80.0;

    public IEnumerable<OsuSkin> SkinsToModify { get; set; }

    public bool SmoothTrail { get; set; }

    public bool Instafade { get; set; }

    protected override bool CacheOriginalElements => true;

    protected override void PopulateTasks()
    {
        var flattenedOptions = FlattenedBottomLevelOptions;
        foreach (OsuSkin skin in SkinsToModify)
        {
            new Operation(
                type: OperationType.SkinModifier,
                targetSkin: skin,
                action: () => ModifySingleSkin(skin, flattenedOptions),
                undoAction: () =>
                {
                    Settings.Log($"Beginning skin modify undo for skin: {skin.Name}");

                    foreach (var pair in OriginalElementsCache)
                    {
                        string fullFilePath = pair.Key;
                        using MemoryStream memoryStream = pair.Value;
                        using FileStream fileStream = File.OpenWrite(fullFilePath);

                        Settings.Log($"Restoring: {fullFilePath} ");

                        memoryStream.Position = 0;
                        memoryStream.CopyTo(fileStream);
                    }

                    OsuData.InvokeSkinModified(skin);
                    Settings.Log($"Finished skin modify undo for skin: {skin.Name}");
                }
            )
            .RunOperation().Wait();
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
                MakeCursorTrailSmooth(workingSkin);
                MakeCursorTrailSmooth(workingSkin, "@2x");
            });
        }

        if (Instafade)
        {
            AddTask(() =>
            {
                MakeCirclesInstafade(workingSkin);
                MakeCirclesInstafade(workingSkin, "@2x");
            });
        }

        string skinIniDestination = $"{workingSkin.Directory.FullName}/skin.ini";
        AddTask(() =>
        {
            Settings.Log($"Writing to {skinIniDestination}");
            File.WriteAllText(skinIniDestination, workingSkin.SkinIni?.ToString());
        });

        Settings.Log($"Skin modification for '{workingSkin.Name}' has completed.");
    }

    private static void MakeCursorTrailSmooth(OsuSkin workingSkin, string suffix = null)
    {
        Settings.Log($"Making cursor{suffix} trail smooth for skin '{workingSkin.Name}'");

        string cursorPath = $"{workingSkin.Directory.FullName}/cursor{suffix}.png";
        string cursorTrailPath = $"{workingSkin.Directory.FullName}/cursortrail{suffix}.png";

        if (!File.Exists(cursorPath) || !File.Exists(cursorTrailPath))
        {
            Settings.Log("Cursor image not found, skipping smooth trail option.");
            return;
        }

        File.Move(cursorPath, $"{workingSkin.Directory.FullName}/cursormiddle{suffix}.png", true);
        File.WriteAllBytes(cursorPath, TransparentPngFile);

        using Image<Rgba32> cursorTrail = Image.Load<Rgba32>(cursorTrailPath);

        Rectangle cropRectangle = Tools.GetContentRectFromImage(cursorTrail);

        if (cropRectangle == Rectangle.Empty)
        {
            Settings.Log("Cursor trail image is empty, skipping smooth trail option.");
            return;
        }

        cursorTrail.Mutate(ctx => ctx.Crop(cropRectangle));
        cursorTrail.Save(cursorTrailPath);
    }

    private static void MakeCirclesInstafade(OsuSkin workingSkin, string suffix = null)
    {
        // With help from https://skinship.xyz/guides/insta_fade_hc.html
        Settings.Log($"Making circles{suffix} instafade for skin '{workingSkin.Name}'");

        string skinDirectory = workingSkin.Directory.FullName;
        string hitcirclePath = $"{skinDirectory}/hitcircle{suffix}.png";
        string hitcircleoverlayPath = $"{skinDirectory}/hitcircleoverlay{suffix}.png";

        OsuSkinIniSection fontsSection = workingSkin.SkinIni?.Sections.Find(s => s.Name == "Fonts");
        OsuSkinIniSection coloursSection = workingSkin.SkinIni?.Sections.Find(s => s.Name == "Colours");

        fontsSection.TryGetValue("HitCirclePrefix", out string hitcirclePrefix);
        hitcirclePrefix = hitcirclePrefix != null ? $"{skinDirectory}/{hitcirclePrefix}" : $"{skinDirectory}/default";

        using Image<Rgba32> hitcircle = File.Exists(hitcirclePath)
            ? Image.Load<Rgba32>(hitcirclePath)
            : Image.Load<Rgba32>(GetDefaultElementBytes($"hitcircle{suffix}.png"));

        using Image<Rgba32> hitcircleoverlay = File.Exists(hitcircleoverlayPath)
            ? Image.Load<Rgba32>(hitcircleoverlayPath)
            : Image.Load<Rgba32>(GetDefaultElementBytes($"hitcircleoverlay{suffix}.png"));

        hitcircleoverlay.Mutate(i => i.Resize((int)(hitcircleoverlay.Width * 1.25), (int)(hitcircleoverlay.Height * 1.25)));
        hitcircle.Mutate(i => i.Resize((int)(hitcircle.Width * 1.25), (int)(hitcircle.Height * 1.25)));

        Godot.Color[] comboColors = workingSkin.ComboColors;

        // Modulate the image with the combo color.
        Godot.Color color = comboColors.Length > 1 ? comboColors[1] : comboColors[0];

        for (int x = 0; x < hitcircle.Width; x++)
        {
            for (int y = 0; y < hitcircle.Height; y++)
            {
                Rgba32 oldColor = hitcircle[x, y];
                byte red = (byte)(256.0f * (oldColor.R / 256.0f) * color.R);
                byte green = (byte)(256.0f * (oldColor.G / 256.0f) * color.G);
                byte blue = (byte)(256.0f * (oldColor.B / 256.0f) * color.B);

                hitcircle[x, y] = Color.FromRgba(red, green, blue, oldColor.A);
            }
        }

        for (int i = 0; i <= 9; i++)
        {
            string defaultXPath = $"{hitcirclePrefix}-{i}{suffix}.png";

            using Image<Rgba32> defaultX = File.Exists(defaultXPath)
                ? Image.Load<Rgba32>(defaultXPath)
                : Image.Load<Rgba32>(GetDefaultElementBytes($"default-{i}{suffix}.png"));

            using Image<Rgba32> newDefaultX = hitcircle.Clone();

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

        if (fontsSection != null)
        {
            // Prevents hitcircles from appearing incorrectly when the combo is greater than 10.
            fontsSection.Remove("HitCircleOverlap");
            fontsSection.Add("HitCircleOverlap", hitcircle.Width.ToString());
        }

        if (coloursSection != null)
        {
            // Make approach circle color the same as the hitcircle color.
            for (int i = 1; i <= comboColors.Length; i++)
            {
                if (i > 2)
                    coloursSection.Remove($"Combo{i}");
            }

            coloursSection.TryGetValue("Combo2", out string combo2);
            coloursSection["Combo1"] = combo2 ?? "0, 192, 0";
        }
    }

    private static byte[] GetDefaultElementBytes(string filename)
        => Godot.FileAccess.GetFileAsBytes($"res://assets/defaultskin/{filename}");

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