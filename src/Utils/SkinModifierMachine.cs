namespace OsuSkinMixer.Utils;

using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Components;
using System.Runtime.Serialization;

/// <summary>Provides methods to modify one or more skins based on a list of <see cref="SkinOption"/> and extra flags.</summary>
public class SkinModifierMachine : SkinMachine
{
    public const double UNCANCELLABLE_AFTER = 80.0;

    public OsuSkin[] SkinsToModify { get; set; }

    public Dictionary<string, Godot.Color[]> SkinComboColourOverrides { get; set; }

    public Dictionary<string, string> SkinCursorColourOverrideImageDirs { get; set; }

    public bool SmoothTrail { get; set; }

    public bool Instafade { get; set; }

    public bool DisableInterfaceAnimations { get; set; }

    protected override bool CacheOriginalElements => true;

    protected override void PopulateTasks()
    {
        OsuData.SweepPaused = true;

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

                        if (!fullFilePath.StartsWith(skin.Directory.FullName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        Settings.Log($"Restoring: {fullFilePath} ");

                        MemoryStream memoryStream = pair.Value;

                        if (memoryStream == null)
                        {
                            if (File.Exists(fullFilePath))
                                File.Delete(fullFilePath);

                            continue;
                        }

                        // Don't leave remnants of data from previous file.
                        File.WriteAllBytes(fullFilePath, Array.Empty<byte>());

                        FileStream fileStream = File.OpenWrite(fullFilePath);
                        memoryStream.Position = 0;
                        memoryStream.CopyTo(fileStream);

                        memoryStream.Dispose();
                        fileStream.Dispose();
                        OriginalElementsCache.Remove(pair.Key);
                    }

                    OsuData.InvokeSkinModified(skin);
                    Settings.Log($"Finished skin modify undo for skin: {skin.Name}");
                }
            )
            .RunOperation(false).Wait();
            CancellationToken.ThrowIfCancellationRequested();
        }

        Progress = UNCANCELLABLE_AFTER;
    }

    protected override void PostRun()
    {
        foreach (OsuSkin skin in SkinsToModify)
        {
            try
            {
                GenerateCreditsFile(skin);
            }
            catch (Exception e)
            {
                Settings.PushException(new InvalidOperationException($"Failed to generate at least one credits file. The skins was still created successfully, don't worry.", e));
            }

            OsuData.InvokeSkinModified(skin);
        }

        OsuData.SweepPaused = false;
    }

    private void ModifySingleSkin(OsuSkin workingSkin, IEnumerable<SkinOption> flattenedOptions)
    {
        Log($"Beginning skin modification for single skin '{workingSkin.Name}'");

        double progressInterval = UNCANCELLABLE_AFTER / SkinsToModify.Count() / flattenedOptions.Count(o => o.Value.Type != SkinOptionValueType.Unchanged);
        foreach (var option in flattenedOptions)
        {
            Log($"About to copy option '{option.Name}' set to {option.Value.Type}, skin '{option.Value.CustomSkin?.Name ?? "null"}'");

            // User wants this skin element to be unchanged.
            if (option.Value.Type == SkinOptionValueType.Unchanged || option.Value.CustomSkin == workingSkin)
                continue;

            CopyOption(workingSkin, option);
            Progress += progressInterval;

            CancellationToken.ThrowIfCancellationRequested();
        }

        AddTask(() => OverrideComboColour(workingSkin));
        AddTask(() => OverrideCursorColour(workingSkin));

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
                MakeCirclesInstafade(workingSkin, "@2x");
                MakeCirclesInstafade(workingSkin);
            });
        }

        if (DisableInterfaceAnimations)
        {
            AddTask(() => MakeInterfaceAnimationsDisabled(workingSkin));
        }

        string skinIniDestination = $"{workingSkin.Directory.FullName}/skin.ini";
        AddFileToOriginalElementsCache(skinIniDestination);

        // Hotfix for case-sensitive file systems.
        if (File.Exists($"{workingSkin.Directory.FullName}/Skin.ini"))
            File.Delete($"{workingSkin.Directory.FullName}/Skin.ini");

        AddTask(() =>
        {
            Log($"Writing to {skinIniDestination}");
            File.WriteAllText(skinIniDestination, workingSkin.SkinIni?.ToString());
        });

        Log($"Skin modification for '{workingSkin.Name}' has completed.");
    }

    private void OverrideComboColour(OsuSkin workingSkin)
    {
        SkinComboColourOverrides.TryGetValue(workingSkin.Name, out Godot.Color[] comboColours);

        if (comboColours == null)
            return;
        
        Log($"Overriding combo colours for skin '{workingSkin.Name}'");
        
        OsuSkinIniSection coloursSection = workingSkin.SkinIni?.Sections.Find(s => s.Name == "Colours");

        for (int i = 0; i < 7; i++)
        {
            if (i == comboColours.Length - 1)
            {
                string lastColor = GodotColorToRgbString(comboColours[i]);

                // Set Combo1 to the last color.
                coloursSection["Combo1"] = lastColor;

                // If there's only one color, set Combo2 to the last color as well.
                if (comboColours.Length == 1)
                    coloursSection["Combo2"] = lastColor;
                    
                continue;
            }

            if (i >= comboColours.Length)
            {
                // Remove any existing combo colours that we don't want anymore.
                coloursSection.Remove($"Combo{i + 1}");
                continue;
            }

            coloursSection[$"Combo{i + 2}"] = GodotColorToRgbString(comboColours[i]);
        }
    }

    private void OverrideCursorColour(OsuSkin workingSkin)
    {
        if (!SkinCursorColourOverrideImageDirs.TryGetValue(workingSkin.Name, out string cursorImageDir))
            return;

        Log($"Overriding cursor colour for skin '{workingSkin.Name}' with directory '{cursorImageDir}'");

        foreach (string file in Directory.EnumerateFiles(cursorImageDir))
        {
            string dest = Path.Combine(workingSkin.Directory.FullName, Path.GetFileName(file));
            Log($"Cursor file '{file}' -> '{dest}'");
            AddFileToOriginalElementsCache(dest);
            File.Move(file, dest, true);
        }
    }

    private string GodotColorToRgbString(Godot.Color color)
        => $"{(int)(color.R * 255)},{(int)(color.G * 255)},{(int)(color.B * 255)}";

    private void MakeCursorTrailSmooth(OsuSkin workingSkin, string suffix = null)
    {
        Log($"Making cursor{suffix} trail smooth for skin '{workingSkin.Name}'");

        string cursorPath = $"{workingSkin.Directory.FullName}/cursor{suffix}.png";
        string cursorTrailPath = $"{workingSkin.Directory.FullName}/cursortrail{suffix}.png";
        string cursorMiddlePath = $"{workingSkin.Directory.FullName}/cursormiddle{suffix}.png";

        if (!File.Exists(cursorPath) || !File.Exists(cursorTrailPath))
        {
            Log("Cursor image not found, skipping smooth trail option.");
            return;
        }

        using Image<Rgba32> cursorTrail = Image.Load<Rgba32>(cursorTrailPath);

        Rectangle cropRectangle = Tools.GetContentRectFromImage(cursorTrail);

        if (cropRectangle == Rectangle.Empty)
        {
            Log("Cursor trail image is empty, skipping smooth trail option.");
            return;
        }

        AddFileToOriginalElementsCache(cursorPath);
        AddFileToOriginalElementsCache(cursorMiddlePath);
        AddFileToOriginalElementsCache(cursorTrailPath);

        cursorTrail.Mutate(ctx => ctx.Crop(cropRectangle));
        cursorTrail.Save(cursorTrailPath);

        File.Move(cursorPath, cursorMiddlePath, true);
        File.WriteAllBytes(cursorPath, TransparentPngFile);
    }

    private void MakeCirclesInstafade(OsuSkin workingSkin, string suffix = null)
    {
        // With help from https://skinship.xyz/guides/insta_fade_hc.html
        Log($"Making circles{suffix} instafade for skin '{workingSkin.Name}'");

        string skinDirectory = workingSkin.Directory.FullName;
        string hitcirclePath = $"{skinDirectory}/hitcircle{suffix}.png";
        string hitcircleoverlayPath = $"{skinDirectory}/hitcircleoverlay{suffix}.png";

        OsuSkinIniSection fontsSection = workingSkin.SkinIni?.Sections.Find(s => s.Name == "Fonts");
        OsuSkinIniSection coloursSection = workingSkin.SkinIni?.Sections.Find(s => s.Name == "Colours");

        string hitcirclePrefix = null;
        fontsSection?.TryGetValue("HitCirclePrefix", out hitcirclePrefix);

        hitcirclePrefix = hitcirclePrefix != null ? $"{skinDirectory}/{hitcirclePrefix}" : $"{skinDirectory}/default";

        int hitcirclePostScale = 1;
        int hitcircleoverlayPostScale = 1;

        // Cretae @2x resolution if not found.
        if (!File.Exists(hitcirclePath) && File.Exists($"{skinDirectory}/hitcircle.png"))
        {  
            File.Copy($"{skinDirectory}/hitcircle.png", hitcirclePath);
            hitcirclePostScale = 2;
        }

        // Cretae @2x resolution if not found.
        if (!File.Exists(hitcircleoverlayPath) && File.Exists($"{skinDirectory}/hitcircleoverlay.png"))
        {
            File.Copy($"{skinDirectory}/hitcircleoverlay.png", hitcircleoverlayPath);
            hitcircleoverlayPostScale = 2;
        }

        using Image<Rgba32> hitcircle = File.Exists(hitcirclePath)
            ? Image.Load<Rgba32>(hitcirclePath)
            : Image.Load<Rgba32>(GetDefaultElementBytes($"hitcircle{suffix}.png"));

        using Image<Rgba32> hitcircleoverlay = File.Exists(hitcircleoverlayPath)
            ? Image.Load<Rgba32>(hitcircleoverlayPath)
            : Image.Load<Rgba32>(GetDefaultElementBytes($"hitcircleoverlay{suffix}.png"));

        hitcircleoverlay.Mutate(i => i.Resize((int)(hitcircleoverlay.Width * hitcircleoverlayPostScale * 1.25), (int)(hitcircleoverlay.Height * hitcircleoverlayPostScale * 1.25)));
        hitcircle.Mutate(i => i.Resize((int)(hitcircle.Width * hitcirclePostScale * 1.25), (int)(hitcircle.Height * hitcirclePostScale * 1.25)));

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

        int newDefaultXSize = 0;

        for (int i = 0; i <= 9; i++)
        {
            string defaultXPath = $"{hitcirclePrefix}-{i}{suffix}.png";
            int defaultPostScale = 1;

            // Cretae @2x resolution if not found.
            if (!File.Exists(defaultXPath) && File.Exists($"{hitcirclePrefix}-{i}.png"))
            {
                File.Copy($"{hitcirclePrefix}-{i}.png", defaultXPath);
                defaultPostScale = 2;
            }

            using Image<Rgba32> defaultX = File.Exists(defaultXPath)
                ? Image.Load<Rgba32>(defaultXPath)
                : Image.Load<Rgba32>(GetDefaultElementBytes($"default-{i}{suffix}.png"));
            
            defaultX.Mutate(i => i.Resize(defaultX.Width * defaultPostScale, defaultX.Height * defaultPostScale));

            Image<Rgba32> newDefaultX;
            if (hitcircleoverlay.Width > hitcircle.Width || hitcircleoverlay.Height > hitcircle.Height)
            {
                newDefaultX = new Image<Rgba32>(hitcircleoverlay.Width, hitcircleoverlay.Height);
            }
            else
            {
                newDefaultX = new Image<Rgba32>(hitcircle.Width, hitcircle.Height);
            }

            newDefaultXSize = newDefaultX.Width;
            newDefaultX.Mutate(img =>
            {
                img.DrawImage(hitcircle, new Point(
                    x: (int)((newDefaultX.Width * 0.5) - (hitcircle.Width * 0.5)),
                    y: (int)((newDefaultX.Height * 0.5) - (hitcircle.Height * 0.5))
                ), 1);
                img.DrawImage(hitcircleoverlay, new Point(
                    x: (int)((newDefaultX.Width * 0.5) - (hitcircleoverlay.Width * 0.5)),
                    y: (int)((newDefaultX.Height * 0.5) - (hitcircleoverlay.Height * 0.5))
                ), 1);
                img.DrawImage(defaultX, new Point(
                    x: (int)((newDefaultX.Width * 0.5) - (defaultX.Width * 0.5)),
                    y: (int)((newDefaultX.Height * 0.5) - (defaultX.Height * 0.5))
                ), 1);
            });

            if (i == 0)
                newDefaultX.Mutate(i => i.Opacity(0));

            AddFileToOriginalElementsCache(defaultXPath);
            newDefaultX.Save($"{hitcirclePrefix}-{i}{suffix}.png");
            newDefaultX.Dispose();
        }

        hitcircle.Mutate(i => i.Opacity(0));
        hitcircleoverlay.Mutate(i => i.Opacity(0));

        AddFileToOriginalElementsCache(hitcirclePath);
        AddFileToOriginalElementsCache(hitcircleoverlayPath);
        hitcircle.Save(hitcirclePath);
        hitcircleoverlay.Save(hitcircleoverlayPath);

        if (fontsSection != null && suffix == null)
        {
            // Prevents hitcircles from appearing incorrectly when the combo is greater than 10.
            // Only do this for the @1x elements, the @2x elements are scaled up by osu!.
            fontsSection.Remove("HitCircleOverlap");
            fontsSection.Add("HitCircleOverlap", newDefaultXSize.ToString());
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

    private void MakeInterfaceAnimationsDisabled(OsuSkin workingSkin)
    {
        Log($"Disabling interface animations for skin '{workingSkin.Name}'");

        ParentSkinOption interfaceOption = (ParentSkinOption)SkinOption.Default[0];
        IEnumerable<SkinFileOption> animatableInterfaceSkinOptions = SkinOption.Flatten(
            interfaceOption.Children).OfType<SkinFileOption>().Where(o => o.IsAnimatable);

        Dictionary<SkinFileOption, Dictionary<FileInfo, int>> animationFrames = new();

        foreach (FileInfo file in workingSkin.Directory.EnumerateFiles())
        {
            SkinFileOption fileOption = animatableInterfaceSkinOptions.FirstOrDefault(
                o => file.Name.StartsWith(o.IncludeFileName + '-', StringComparison.OrdinalIgnoreCase));

            if (fileOption == null)
                continue;

            string frameIndexString = file.Name[(file.Name.LastIndexOf('-') + 1)..file.Name.LastIndexOf('.')].TrimSuffix("@2x");
            if (!int.TryParse(frameIndexString, out int frameIndex))
                continue;

            animationFrames.TryAdd(fileOption, new());
            animationFrames[fileOption].Add(file, frameIndex);
        }

        foreach (var pair in animationFrames)
        {
            // We need to choose one frame to keep, so we'll choose the middle frame. Divide by 4 to account for @2x frames.
            int keepFrame = pair.Value.Count / 4;

            foreach (var framePair in pair.Value)
            {
                FileInfo file = framePair.Key;
                int frameIndex = framePair.Value;

                if (frameIndex == keepFrame)
                {
                    Log($"Keeping frame '{file.Name}'");
                    file.MoveTo(Path.Combine(file.DirectoryName, file.Name.Replace($"-{frameIndex}", "")), true);
                    continue;
                }

                Log($"Deleting frame '{file.Name}'");
                AddFileToOriginalElementsCache(file.FullName);
                file.Delete();
            }
        }
    }

    private static byte[] GetDefaultElementBytes(string filename)
    {
        var texture = Godot.GD.Load<Godot.Texture2D>($"res://assets/defaultskin/{filename}");
        return texture.GetImage().SavePngToBuffer();
    }

    protected override void CopyIniPropertyOption(OsuSkin workingSkin, SkinIniPropertyOption iniPropertyOption)
    {
        var property = iniPropertyOption.IncludeSkinIniProperty;

        // Remove the skin.ini properties to avoid remnants when using skin modifier.
        AddPriorityTask(() =>
        {
            if (workingSkin.SkinIni?.Sections.LastOrDefault(s => s.Name == property.section)?.Remove(property.property) == true)
                Log($"Removed skin.ini property '{property.section}.{property.property}' to avoid remnants");
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
            if (workingSkin.SkinIni?.Sections.Remove(section) == true)
                Log($"Removed skin.ini section '{iniSectionOption.SectionName}' where '{iniSectionOption.Property.Key}: {iniSectionOption.Property.Value}' to avoid remnants");
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
                Log($"Removing file '{file.FullName}' to avoid remnants");
                AddFileToOriginalElementsCache(file.FullName);
                file.Delete();
            }
        });

        base.CopyFileOption(workingSkin, fileOption);
    }
}