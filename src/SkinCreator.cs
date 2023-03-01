using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer;

public class SkinCreator
{
    public const string WORKING_DIR_NAME = ".osu-skin-mixer_working-skin";

    public string Name { get; set; }

    public SkinOption[] SkinOptions { get; set; }

    public float? Progress { get; private set; }

    public string Status { get; set; }

    public Action<float, string> ProgressChangedAction { get; set; }

    private readonly List<OsuSkinWithFiles> CachedSkinWithFiles = new();

    private List<FileInfo> WorkingSkinOriginalFiles;

    private OsuSkinIni NewSkinIni;

    private DirectoryInfo NewSkinDir;

    public static void TriggerOskImport(OsuSkin skin)
    {
        string oskDestPath = $"{Settings.SkinsFolderPath}/{skin.Name}.osk";
        GD.Print($"Importing skin into game from '{oskDestPath}'");

        // osu! will handle the empty .osk (zip) file by switching the current skin to the skin with name `newSkinName`.
        File.WriteAllBytes(oskDestPath, new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        Godot.OS.ShellOpen(oskDestPath);
    }

    public async Task<OsuSkin> CreateAndImportAsync(CancellationToken cancellationToken)
    {
        GD.Print($"Beginning skin creation with name '{Name}'");

        Progress = 0;
        Status = "Preparing";

        NewSkinIni = new OsuSkinIni(Name, "osu! skin mixer by rednir");
        NewSkinDir = Directory.CreateDirectory($"{Path.GetTempPath()}/{WORKING_DIR_NAME}");

        // There might be skin elements from a failed attempt still in the directory.
        foreach (var file in NewSkinDir.EnumerateFiles())
            file.Delete();

        var flattenedOptions = SkinOption.Flatten(SkinOptions).Where(o => o is not ParentSkinOption);

        // Progress should not take into account options set to use the
        // default skin as no work needs to be done for those options.
        float progressInterval = 100f / flattenedOptions.Count(o => o.Skin != null);

        foreach (var option in flattenedOptions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GD.Print($"About to copy option '{option.Name}' set to '{option.Skin?.Name ?? "null"}'");
            Status = $"Copying: {option.Name}";
            ProgressChangedAction?.Invoke(Progress.Value, Status);

            // User wants default skin elements to be used.
            if (option.Skin == null)
                continue;

            CopyOption(option);
            Progress += progressInterval;
        }

        await File.WriteAllTextAsync($"{NewSkinDir.FullName}/skin.ini", NewSkinIni.ToString(), cancellationToken);

        Status = "Importing...";

        string dirDestPath = $"{Settings.SkinsFolderPath}/{Name}";
        GD.Print($"Copying working folder to '{dirDestPath}'");

        if (Directory.Exists(dirDestPath))
            Directory.Delete(dirDestPath, true);

        NewSkinDir.MoveTo(dirDestPath);

        OsuSkin skin = new(new DirectoryInfo(dirDestPath));
        OsuData.AddSkin(skin);

        Progress = 100;
        GD.Print($"Skin creation for '{Name}' has completed.");

        return skin;
    }

    public void ModifySkins(IEnumerable<OsuSkin> skinsToModify, CancellationToken cancellationToken)
    {
        int skinCount = skinsToModify.Count();

        GD.Print($"Beginning skin modification for {skinCount} skins.");
        Progress = 0;

        foreach (OsuSkin skin in skinsToModify)
        {
            Status = $"Modifying: {skin.Name}";

            cancellationToken.ThrowIfCancellationRequested();
            ModifySingleSkin(skin);

            Progress += 100f / skinCount;
        }

        Progress = 100;

        GD.Print("Skin modification has completed for all skins.");
    }

    private void ModifySingleSkin(OsuSkin workingSkin)
    {
        GD.Print($"Beginning skin modification for single skin '{workingSkin.Name}'");

        NewSkinDir = workingSkin.Directory;
        NewSkinIni = workingSkin.SkinIni;
        WorkingSkinOriginalFiles = NewSkinDir.GetFiles().ToList();

        IEnumerable<SkinOption> flattenedOptions = SkinOption.Flatten(SkinOptions).Where(o => o is not ParentSkinOption);

        foreach (var option in flattenedOptions)
        {
            GD.Print($"About to copy option '{option.Name}' set to '{option.Skin?.Name ?? "null"}'");
            ProgressChangedAction?.Invoke(Progress.Value, Status);

            // User wants default skin elements to be used.
            if (option.Skin == null)
                continue;

            CopyOption(option);
        }

        File.WriteAllText($"{NewSkinDir.FullName}/skin.ini", NewSkinIni.ToString());

        OsuData.InvokeSkinModified(workingSkin);
        GD.Print($"Skin modification for '{workingSkin.Name}' has completed.");
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

    private void CopyIniPropertyOption(OsuSkinWithFiles skin, SkinIniPropertyOption iniPropertyOption)
    {
        var property = iniPropertyOption.IncludeSkinIniProperty;

        // Remove the skin.ini to avoid remnants when using skin modifier.
        NewSkinIni.Sections.LastOrDefault(s => s.Name == property.section)?.Remove(property.property);

        foreach (var section in skin.SkinIni.Sections)
        {
            if (property.section != section.Name)
                continue;

            foreach (var pair in section)
            {
                if (pair.Key == property.property)
                {
                    OsuSkinIniSection newSkinSection = NewSkinIni.Sections.Last(s => s.Name == section.Name);
                    newSkinSection.Add(
                        key: pair.Key,
                        value: pair.Value);

                    // Check if the skin.ini property value includes any skin elements.
                    // If so, include it in the new skin, (their inclusion takes priority over the elements from matching filenames)
                    CopyFileFromSkinIniProperty(skin, pair);
                }
            }
        }
    }

    private void CopyIniSectionOption(OsuSkinWithFiles skin, SkinIniSectionOption iniSectionOption)
    {
        OsuSkinIniSection section = skin.SkinIni.Sections.Find(
            s => s.Name == iniSectionOption.SectionName && s.Contains(iniSectionOption.Property));

        if (section == null)
            return;

        GD.Print($"Copying skin.ini section '{iniSectionOption.SectionName}' where '{iniSectionOption.Property.Key}: {iniSectionOption.Property.Value}'");

        NewSkinIni.Sections.Add(section);
        foreach (var property in section)
            CopyFileFromSkinIniProperty(skin, property);
    }

    private void CopyFileFromSkinIniProperty(OsuSkinWithFiles skin, KeyValuePair<string, string> property)
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

    private void CopyFileOption(OsuSkinWithFiles skin, SkinFileOption fileOption)
    {
        // Avoid remnants when using skin modifier by removing old files, so
        // the default skin will be used if there is no file match. Bit of a hack.
        if (WorkingSkinOriginalFiles != null)
        {
            foreach (FileInfo file in WorkingSkinOriginalFiles.Where(f => CheckIfFileAndOptionMatch(f, fileOption)).ToArray())
            {
                GD.Print($"'Removing {file.FullName}' to avoid remnants");
                WorkingSkinOriginalFiles.Remove(file);
                file.Delete();
            }
        }

        foreach (var file in skin.Files)
        {
            string filename = Path.GetFileNameWithoutExtension(file.Name);
            string extension = Path.GetExtension(file.Name);

            if (CheckIfFileAndOptionMatch(file, fileOption))
            {
                string newFilePath = $"{NewSkinDir.FullName}/{file.Name}";
                GD.Print($"'{file.FullName}' -> '{newFilePath}' (due to filename match)");

                // If the file already exists, overwrite it, i.e. if we are modifying an existing skin.
                if (File.Exists(newFilePath))
                    File.Delete(newFilePath);

                file.CopyTo(newFilePath);
            }
        }
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

    private OsuSkinWithFiles GetSkinWithFiles(OsuSkin skin)
    {
        var existing = CachedSkinWithFiles.Find(s => s.Name == skin.Name);
        if (existing != null)
            return existing;

        GD.Print($"Caching skin's files for '{skin.Name}'");
        var skinWithFiles = new OsuSkinWithFiles(skin);
        CachedSkinWithFiles.Add(skinWithFiles);

        return skinWithFiles;
    }
}