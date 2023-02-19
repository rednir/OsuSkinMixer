using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Godot;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Statics;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer;

public class SkinCreator
{
    public const string WORKING_DIR_NAME = ".osu-skin-mixer_working-skin";

    public string Name { get; set; }

    public SkinOption[] SkinOptions { get; set; }

    public float? Progress { get; private set; }

    public string Status { get; set; }

    private readonly List<SkinWithFiles> CachedSkinWithFiles = new();

    private SkinIni NewSkinIni;

    private DirectoryInfo NewSkinDir;

    public void Create(CancellationToken cancellationToken)
    {
        // if (Progress != null)
        //     throw new SkinCreationInvalidException("Skin creator has been used or is in use.");

        // if (string.IsNullOrWhiteSpace(Name))
        //     throw new SkinCreationInvalidException("Set a name for the new skin first.");

        // if (Name.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
        //     throw new SkinCreationInvalidException("The skin name contains invalid symbols.");

        // if (Directory.Exists(Settings.Content.SkinsFolder + "/" + Name) && !overwrite)
        //     throw new SkinExistsException();

        CreateSkin(cancellationToken);
        Progress = 100;
    }

    public void TriggerOskImport()
    {
        string oskDestPath = $"{Settings.Content.SkinsFolder}/{Name}.osk";
        GD.Print($"Importing skin into game from '{oskDestPath}'");

        // osu! will handle the empty .osk (zip) file by switching the current skin to the skin with name `newSkinName`.
        File.WriteAllBytes(oskDestPath, new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        Godot.OS.ShellOpen(oskDestPath);
    }

    private void CreateSkin(CancellationToken cancellationToken)
    {
        GD.Print($"Beginning skin creation with name '{Name}'");

        Progress = 0;
        Status = "Preparing";

        NewSkinIni = new SkinIni(Name, "osu! skin mixer by rednir");
        NewSkinDir = Directory.CreateDirectory($"{Settings.Content.SkinsFolder}/{WORKING_DIR_NAME}");

        // There might be skin elements from a failed attempt still in the directory.
        foreach (var file in NewSkinDir.EnumerateFiles())
            file.Delete();

        var flattenedOptions = SkinOption.Flatten(SkinOptions).Where(o => !(o is ParentSkinOption));

        // Progress should not take into account options set to use the
        // default skin as no work needs to be done for those options.
        float progressInterval = 100f / flattenedOptions.Count(o => o.Skin != null);

        foreach (var option in flattenedOptions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GD.Print($"About to copy option '{option.Name}' set to '{option.Skin?.Name ?? "null"}'");
            Status = $"Copying: {option.Name}";

            // User wants default skin elements to be used.
            if (option.Skin == null)
                continue;

            CopyOption(option);
            Progress += progressInterval;
        }

        File.WriteAllText($"{NewSkinDir.FullName}/skin.ini", NewSkinIni.ToString());

        Status = "Importing...";

        string dirDestPath = $"{Settings.Content.SkinsFolder}/{Name}";
        GD.Print($"Copying working folder to '{dirDestPath}'");

        if (Directory.Exists(dirDestPath))
            Directory.Delete(dirDestPath, true);

        NewSkinDir.MoveTo(dirDestPath);

        GD.Print($"Skin creation for '{Name}' has completed.");
    }

    private void CopyOption(SkinOption option)
    {
        var skin = GetSkinWithFiles(option.Skin);

        if (skin.SkinIni != null)
        {
            if (option is SkinIniPropertyOption iniPropertyOption)
                CopyIniPropertyOption(skin, iniPropertyOption);
            else if (option is SkinIniSectionOption iniSectionOption)
                CopyIniSectionOption(skin, iniSectionOption);
        }

        if (option is SkinFileOption fileOption)
            CopyFileOption(skin, fileOption);
    }

    private void CopyIniPropertyOption(SkinWithFiles skin, SkinIniPropertyOption iniPropertyOption)
    {
        var property = iniPropertyOption.IncludeSkinIniProperty;

        foreach (var section in skin.SkinIni.Sections)
        {
            if (property.section != section.Name)
                continue;

            foreach (var pair in section)
            {
                if (pair.Key == property.property)
                {
                    NewSkinIni.Sections.Last(s => s.Name == section.Name).Add(
                        key: pair.Key,
                        value: pair.Value);

                    // Check if the skin.ini property value includes any skin elements.
                    // If so, include it in the new skin, (their inclusion takes priority over the elements from matching filenames)
                    CopyFileFromSkinIniProperty(skin, pair);
                }
            }
        }
    }

    private void CopyIniSectionOption(SkinWithFiles skin, SkinIniSectionOption iniSectionOption)
    {
        SkinIniSection section = skin.SkinIni.Sections.Find(
            s => s.Name == iniSectionOption.SectionName && s.Contains(iniSectionOption.Property));

        if (section == null)
            return;

        GD.Print($"Copying skin.ini section '{iniSectionOption.SectionName}' where '{iniSectionOption.Property.Key}: {iniSectionOption.Property.Value}'");

        NewSkinIni.Sections.Add(section);
        foreach (var property in section)
            CopyFileFromSkinIniProperty(skin, property);
    }

    private void CopyFileFromSkinIniProperty(SkinWithFiles skin, KeyValuePair<string, string> property)
    {
        if (!SkinIni.PropertyHasFilePath(property.Key))
            return;

        int lastSlashIndex = property.Value.LastIndexOf('/');
        string prefixPropertyDirPath = lastSlashIndex >= 0 ? property.Value[..lastSlashIndex] : null;
        string prefixPropertyFileName = property.Value[(lastSlashIndex + 1)..];

        // If `prefixPropertyDirPath` is null, the path is the skin folder root which obviously exists.
        if (prefixPropertyDirPath != null && !Directory.Exists($"{skin.Directory.FullName}/{prefixPropertyDirPath}"))
            return;

        // In that case, better to use the existing file collection that we have instead of creating another one.
        IEnumerable<FileInfo> files = prefixPropertyDirPath == null ?
            skin.Files : new DirectoryInfo($"{skin.Directory.FullName}/{prefixPropertyDirPath}").EnumerateFiles();

        var fileDestDir = Directory.CreateDirectory($"{NewSkinDir}/{prefixPropertyDirPath}");
        foreach (var file in files)
        {
            if (file.Name.StartsWith(prefixPropertyFileName, StringComparison.OrdinalIgnoreCase))
            {
                GD.Print($"'{file.FullName}' -> '{fileDestDir.FullName}' (due to skin.ini)");
                file.CopyTo($"{fileDestDir.FullName}/{file.Name}", true);
            }
        }
    }

    private void CopyFileOption(SkinWithFiles skin, SkinFileOption fileOption)
    {
        foreach (var file in skin.Files)
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
                    GD.Print($"'{file.FullName}' -> '{NewSkinDir.FullName}/{file.Name}' (due to filename match)");

                    try
                    {
                        file.CopyTo($"{NewSkinDir.FullName}/{file.Name}");
                    }
                    catch (IOException)
                    {
                        GD.Print("...but it failed, probably because the file already exists.");
                    }
                }
            }
        }
    }

    private SkinWithFiles GetSkinWithFiles(Skin skin)
    {
        var existing = CachedSkinWithFiles.Find(s => s.Name == skin.Name);
        if (existing != null)
            return existing;

        GD.Print($"Caching skin's files for '{skin.Name}'");
        var skinWithFiles = new SkinWithFiles(skin);
        CachedSkinWithFiles.Add(skinWithFiles);

        return skinWithFiles;
    }
}