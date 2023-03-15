using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Utils;

/// <summary>Base for classes that peform tasks based on a list of <see cref="SkinOption"/>. Provides abstract methods for populating tasks to be peformed on the relevant skin folders.</summary>
public abstract class SkinMachine : IDisposable
{
    protected static byte[] TransparentPngFile => new byte[] {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x04, 0x00, 0x00, 0x00, 0xB5, 0x1C, 0x0C,
        0x02, 0x00, 0x00, 0x00, 0x0B, 0x49, 0x44, 0x41, 0x54, 0x78, 0xDA, 0x63, 0x64, 0x60, 0x00, 0x00,
        0x00, 0x06, 0x00, 0x02, 0x30, 0x81, 0xD0, 0x2F, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
        0xAE, 0x42, 0x60, 0x82
    };

    /// <summary>The skin options that will be used to populate the tasks.</summary>
    public SkinOption[] SkinOptions { get; set; }

    /// <summary>Represents the progress of the operation as a value between 0 and 100 or null if there is no ongoing operation..</summary>
    public double? Progress
    {
        get
        {
            return _progress;
        }
        protected set
        {
            if (value == null)
                _progress = null;
            else if (value < 0)
                _progress = 0;
            else if (value > 100)
                _progress = 100;
            else
                _progress = value;

            ProgressChanged?.Invoke(_progress ?? 0);
        }
    }

    public Action<double> ProgressChanged { get; set; }

    protected virtual bool CacheOriginalElements => false;

    protected Dictionary<string, MemoryStream> OriginalElementsCache { get; } = new();

    protected CancellationToken CancellationToken { get; set; }

    protected IEnumerable<SkinOption> FlattenedBottomLevelOptions => SkinOption.Flatten(SkinOptions).Where(o => o is not ParentSkinOption);

    private readonly List<Action> _tasks = new();

    private double? _progress;

    private bool _disposedValue;

    public void Run(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        OriginalElementsCache.Clear();
        _tasks.Clear();

        Settings.Log("Started");
        Stopwatch stopwatch = new();
        stopwatch.Start();

        Progress = 0;

        PopulateTasks();
        RunAllTasks();

        Progress = 100;

        PostRun();

        stopwatch.Stop();
        Settings.Log($"Finished in {stopwatch.Elapsed.TotalSeconds}s");
        Progress = null;
    }

    protected abstract void PopulateTasks();

    private void RunAllTasks()
    {
        double progressInterval = (100.0 - Progress.Value) / _tasks.Count;
        foreach (Action task in _tasks)
        {
            Progress += progressInterval;
            task();
        }
    }

    protected abstract void PostRun();

    protected void CopyOption(OsuSkin workingSkin, SkinOption option)
    {
        switch (option)
        {
            case SkinIniPropertyOption iniPropertyOption:
                CopyIniPropertyOption(workingSkin, iniPropertyOption);
                break;
            case SkinIniSectionOption iniSectionOption:
                CopyIniSectionOption(workingSkin, iniSectionOption);
                break;
            case SkinFileOption fileOption:
                CopyFileOption(workingSkin, fileOption);
                break;
        }
    }

    protected virtual void CopyIniPropertyOption(OsuSkin workingSkin, SkinIniPropertyOption iniPropertyOption)
    {
        if (iniPropertyOption.Value.Type == SkinOptionValueType.DefaultSkin || iniPropertyOption.Value.CustomSkin?.SkinIni == null)
            return;

        foreach (var section in iniPropertyOption.Value.CustomSkin.SkinIni.Sections)
        {
            if (iniPropertyOption.IncludeSkinIniProperty.section != section.Name)
                continue;

            foreach (var pair in section)
            {
                if (pair.Key == iniPropertyOption.IncludeSkinIniProperty.property)
                {
                    OsuSkinIniSection newSkinSection = workingSkin.SkinIni.Sections.Last(s => s.Name == section.Name);
                    AddTask(() =>
                    {
                        Settings.Log($"Copying skin.ini property '{section.Name}.{pair.Key}: {pair.Value}'");
                        newSkinSection.Add(
                            key: pair.Key,
                            value: pair.Value);
                    });

                    // Check if the skin.ini property value includes any skin elements.
                    // If so, include it in the new skin, (their inclusion takes priority over the elements from matching filenames)
                    CopyFileFromSkinIniProperty(workingSkin, iniPropertyOption.Value.CustomSkin, pair);
                }
            }
        }
    }

    protected virtual void CopyIniSectionOption(OsuSkin workingSkin, SkinIniSectionOption iniSectionOption)
    {
        if (iniSectionOption.Value.Type == SkinOptionValueType.DefaultSkin || iniSectionOption.Value.CustomSkin?.SkinIni == null)
            return;

        OsuSkinIniSection section = iniSectionOption.Value.CustomSkin.SkinIni.Sections.Find(
            s => s.Name == iniSectionOption.SectionName && s.Contains(iniSectionOption.Property));

        if (section == null)
            return;

        Settings.Log($"Copying skin.ini section '{iniSectionOption.SectionName}' where '{iniSectionOption.Property.Key}: {iniSectionOption.Property.Value}'");

        workingSkin.SkinIni.Sections.Add(section);
        foreach (var property in section)
            CopyFileFromSkinIniProperty(workingSkin, iniSectionOption.Value.CustomSkin, property);
    }

    protected virtual void CopyFileFromSkinIniProperty(OsuSkin workingSkin, OsuSkin skinToCopy, KeyValuePair<string, string> property)
    {
        if (!OsuSkinIni.PropertyHasFilePath(property.Key))
            return;

        int lastSlashIndex = property.Value.LastIndexOf('/');
        string prefixPropertyDirPath = lastSlashIndex >= 0 ? property.Value[..lastSlashIndex] : null;
        string prefixPropertyFileName = property.Value[(lastSlashIndex + 1)..];

        // If `prefixPropertyDirPath` is null, the path is the skin folder root which obviously exists.
        if (prefixPropertyDirPath != null && !Directory.Exists($"{skinToCopy.Directory.FullName}/{prefixPropertyDirPath}"))
            return;

        // In that case, better to use the existing file collection that we have instead of creating another one.
        IEnumerable<FileInfo> files = prefixPropertyDirPath == null ?
            skinToCopy.Directory.EnumerateFiles() : new DirectoryInfo($"{skinToCopy.Directory.FullName}/{prefixPropertyDirPath}").EnumerateFiles();

        var fileDestDir = Directory.CreateDirectory($"{workingSkin.Directory.FullName}/{prefixPropertyDirPath}");
        foreach (var file in files)
        {
            if (file.Name.StartsWith(prefixPropertyFileName, StringComparison.OrdinalIgnoreCase))
                AddCopyFileTask(file, fileDestDir, "due to skin.ini");
        }
    }

    protected virtual void CopyFileOption(OsuSkin workingSkin, SkinFileOption fileOption)
    {
        if (fileOption.Value.Type == SkinOptionValueType.DefaultSkin)
            return;

        if (fileOption.Value.Type == SkinOptionValueType.Blank)
        {
            AddCopyBlankFileTask(fileOption, workingSkin.Directory);
            return;
        }

        foreach (var file in fileOption.Value.CustomSkin.Directory.EnumerateFiles())
        {
            if (CheckIfFileAndOptionMatch(file, fileOption))
                AddCopyFileTask(file, workingSkin.Directory, "due to filename match");
        }
    }

    protected void AddTask(Action task)
        => _tasks.Add(task);

    protected void AddPriorityTask(Action task)
        => _tasks.Insert(0, task);

    protected void AddCopyFileTask(FileInfo file, DirectoryInfo fileDestDir, string reason)
    {
        string destFullPath = $"{fileDestDir.FullName}/{file.Name}";

        // We cache the file data beforehand in case it changes or is deleted before we have the chance to copy it.
        MemoryStream memoryStream = new();
        file.OpenRead().CopyTo(memoryStream);

        AddFileToOriginalElementsCache(destFullPath);

        _tasks.Add(() =>
        {
            Settings.Log($"Run task '{file.FullName}' -> '{destFullPath}' ({reason})");

            using FileStream fileStream = File.Create(destFullPath);
            memoryStream.Position = 0;
            memoryStream.CopyTo(fileStream);

            if (!CacheOriginalElements)
                memoryStream.Dispose();
        });
    }

    protected void AddCopyBlankFileTask(SkinFileOption fileOption, DirectoryInfo fileDestDir)
    {
        string destFullPath = $"{fileDestDir.FullName}/{fileOption.Name.Replace("-*", "").Replace("*", "")}";

        AddFileToOriginalElementsCache(destFullPath);

        if (fileOption.IsAudio)
        {
            _tasks.Add(() =>
            {
                Settings.Log($"Run task (blank file) -> '{destFullPath}'");
                File.Create(destFullPath).Dispose();
            });
        }
        else
        {
            _tasks.Add(() =>
            {
                Settings.Log($"Run task (blank file) -> '{destFullPath}'");

                // This is a 1x1 transparent PNG file. A zero byte file will cause osu! to fall back to the default skin.
                File.WriteAllBytes(destFullPath, TransparentPngFile);
            });
        }
    }

    protected void AddFileToOriginalElementsCache(string fullFilePath)
    {
        if (!CacheOriginalElements)
            return;

        // Cache original element for when after creation has finished, in case an undo operation is requested.
        MemoryStream originalMemoryStream = new();
        if (File.Exists(fullFilePath))
        {
            FileStream originalFileStream = File.OpenRead(fullFilePath);
            originalFileStream.CopyTo(originalMemoryStream);
            originalFileStream.Dispose();
        }

        OriginalElementsCache.TryAdd(fullFilePath, originalMemoryStream);
    }

    protected static bool CheckIfFileAndOptionMatch(FileInfo file, SkinFileOption fileOption)
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                foreach (var pair in OriginalElementsCache)
                    pair.Value.Dispose();
            }

            _tasks.Clear();
            _disposedValue = true;
        }
    }
}