using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer;

public class SkinModifier
{
    public const float UNCANCELLABLE_AFTER = 80f;

    public SkinOption[] SkinOptions { get; set; }

    public float? Progress { get; private set; }

    public Action<float> ProgressChangedAction { get; set; }

    public IEnumerable<OsuSkin> SkinsToModify { get; set; }

    private int _skinCount;

    private readonly List<Action> _copyTasks = new();

    public void ModifySkins(CancellationToken cancellationToken)
    {
        _copyTasks.Clear();
        _skinCount = SkinsToModify.Count();

        GD.Print($"Beginning skin modification for {_skinCount} skins.");
        Progress = 0;

        IEnumerable<SkinOption> flattenedOptions = SkinOption.Flatten(SkinOptions).Where(o => o is not ParentSkinOption);

        foreach (OsuSkin skin in SkinsToModify)
        {
            ModifySingleSkin(skin, flattenedOptions, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        Progress = UNCANCELLABLE_AFTER;

        foreach (Action task in _copyTasks)
        {
            Progress += UNCANCELLABLE_AFTER / _copyTasks.Count;
            task();
        }

        Progress = 100;

        GD.Print("Skin modification has completed for all skins.");
    }

    private void ModifySingleSkin(OsuSkin workingSkin, IEnumerable<SkinOption> flattenedOptions, CancellationToken cancellationToken)
    {
        GD.Print($"Beginning skin modification for single skin '{workingSkin.Name}'");

        float progressInterval = UNCANCELLABLE_AFTER / _skinCount / flattenedOptions.Count(o => o.Skin != null);
        foreach (var option in flattenedOptions)
        {
            GD.Print($"About to copy option '{option.Name}' set to '{option.Skin?.Name ?? "null"}'");

            Progress += progressInterval;
            ProgressChangedAction?.Invoke(Progress.Value);

            // User wants default skin elements to be used.
            if (option.Skin == null)
                continue;

            CopyOption(workingSkin, option);
            cancellationToken.ThrowIfCancellationRequested();
        }

        _copyTasks.Add(() =>
        {
            GD.Print($"Writing skin.ini for '{workingSkin.Name}'");
            File.WriteAllText($"{workingSkin.Directory.FullName}/skin.ini", workingSkin.SkinIni.ToString());
        });

        GD.Print($"Skin modification for '{workingSkin.Name}' has completed.");
    }

    private void CopyOption(OsuSkin workingSkin, SkinOption option)
    {
        var skin = option.Skin;

        if (skin.SkinIni != null)
        {
            if (option is SkinIniPropertyOption iniPropertyOption)
                CopyIniPropertyOption(workingSkin, skin, iniPropertyOption);
            else if (option is SkinIniSectionOption iniSectionOption)
                CopyIniSectionOption(workingSkin, skin, iniSectionOption);
        }

        if (option is SkinFileOption fileOption)
            CopyFileOption(workingSkin, skin, fileOption);
    }

    private void CopyIniPropertyOption(OsuSkin workingSkin, OsuSkin skinToCopy, SkinIniPropertyOption iniPropertyOption)
    {
        var property = iniPropertyOption.IncludeSkinIniProperty;

        // Remove the skin.ini to avoid remnants when using skin modifier.
        workingSkin.SkinIni.Sections.LastOrDefault(s => s.Name == property.section)?.Remove(property.property);

        foreach (var section in skinToCopy.SkinIni.Sections)
        {
            if (property.section != section.Name)
                continue;

            foreach (var pair in section)
            {
                if (pair.Key == property.property)
                {
                    OsuSkinIniSection newSkinSection = workingSkin.SkinIni.Sections.Last(s => s.Name == section.Name);
                    newSkinSection.Add(
                        key: pair.Key,
                        value: pair.Value);

                    // Check if the skin.ini property value includes any skin elements.
                    // If so, include it in the new skin, (their inclusion takes priority over the elements from matching filenames)
                    CopyFileFromSkinIniProperty(workingSkin, skinToCopy, pair);
                }
            }
        }
    }

    private void CopyIniSectionOption(OsuSkin workingSkin, OsuSkin skinToCopy, SkinIniSectionOption iniSectionOption)
    {
        OsuSkinIniSection section = skinToCopy.SkinIni.Sections.Find(
            s => s.Name == iniSectionOption.SectionName && s.Contains(iniSectionOption.Property));

        if (section == null)
            return;

        GD.Print($"Copying skin.ini section '{iniSectionOption.SectionName}' where '{iniSectionOption.Property.Key}: {iniSectionOption.Property.Value}'");

        workingSkin.SkinIni.Sections.Add(section);
        foreach (var property in section)
            CopyFileFromSkinIniProperty(workingSkin, skinToCopy, property);
    }

    private void CopyFileFromSkinIniProperty(OsuSkin workingSkin, OsuSkin skin, KeyValuePair<string, string> property)
    {
        if (!OsuSkinIni.PropertyHasFilePath(property.Key))
            return;

        int lastSlashIndex = property.Value.LastIndexOf('/');
        string prefixPropertyDirPath = lastSlashIndex >= 0 ? property.Value[..lastSlashIndex] : null;
        string prefixPropertyFileName = property.Value[(lastSlashIndex + 1)..];

        // If `prefixPropertyDirPath` is null, the path is the skin folder root which obviously exists.
        if (prefixPropertyDirPath != null && !Directory.Exists($"{skin.Directory.FullName}/{prefixPropertyDirPath}"))
            return;

        // In that case, better to use the existing file collection that we have instead of creating another one.
        IEnumerable<FileInfo> files = prefixPropertyDirPath == null ?
            skin.Directory.EnumerateFiles() : new DirectoryInfo($"{skin.Directory.FullName}/{prefixPropertyDirPath}").EnumerateFiles();

        var fileDestDir = Directory.CreateDirectory($"{workingSkin.Directory.FullName}/{prefixPropertyDirPath}");
        foreach (var file in files)
        {
            if (file.Name.StartsWith(prefixPropertyFileName, StringComparison.OrdinalIgnoreCase))
                AddCopyTask(file, fileDestDir, "due to skin.ini");
        }
    }

    private void CopyFileOption(OsuSkin workingSkin, OsuSkin skinToCopy, SkinFileOption fileOption)
    {
        // Avoid remnants when using skin modifier by removing old files, so
        // the default skin will be used if there is no file match. Bit of a hack.
        _copyTasks.Insert(0, () =>
        {
            foreach (FileInfo file in workingSkin.Directory.GetFiles().Where(f => CheckIfFileAndOptionMatch(f, fileOption)).ToArray())
            {
                GD.Print($"'Removing {file.FullName}' to avoid remnants");
                file.Delete();
            }
        });

        foreach (var file in skinToCopy.Directory.EnumerateFiles())
        {
            string filename = Path.GetFileNameWithoutExtension(file.Name);
            string extension = Path.GetExtension(file.Name);

            if (CheckIfFileAndOptionMatch(file, fileOption))
                AddCopyTask(file, workingSkin.Directory, "due to filename match");
        }
    }

    public void AddCopyTask(FileInfo file, DirectoryInfo fileDestDir, string logDetails)
    {
        string destFullPath = $"{fileDestDir.FullName}/{file.Name}";

        // We cache the file data beforehand in case it changes or is deleted before we have the chance to copy it.
        MemoryStream memoryStream = new();
        file.OpenRead().CopyTo(memoryStream);

        _copyTasks.Add(() =>
        {
            GD.Print($"'{file.FullName}' -> '{destFullPath}' ({logDetails})");

            FileStream fileStream = File.Create(destFullPath);
            memoryStream.Position = 0;
            memoryStream.CopyTo(fileStream);
            memoryStream.Dispose();
            fileStream.Dispose();
        });
    }

    private static bool CheckIfFileAndOptionMatch(FileInfo file, SkinFileOption fileOption)
    {
        string filename = Path.GetFileNameWithoutExtension(file.Name);
        string extension = Path.GetExtension(file.Name);

        // Check for file name match.
        if (
            filename.Equals(fileOption.IncludeFileName, StringComparison.OrdinalIgnoreCase) || filename.Equals(fileOption.IncludeFileName + "@2x", StringComparison.OrdinalIgnoreCase)
            || (fileOption.IncludeFileName.EndsWith("*") && filename.StartsWith(fileOption.IncludeFileName.TrimEnd('*'), StringComparison.OrdinalIgnoreCase))
        )
        {
            // Check for file type match.
            if (
                ((extension == ".png" || extension == ".jpg") && !fileOption.IsAudio)
                || ((extension == ".mp3" || extension == ".ogg" || extension == ".wav") && fileOption.IsAudio)
            )
            {
                return true;
            }
        }

        return false;
    }
}