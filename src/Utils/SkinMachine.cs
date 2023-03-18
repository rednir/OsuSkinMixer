using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Utils;

/// <summary>Base for classes that peform tasks based on a list of <see cref="SkinOption"/>. Provides abstract methods for populating tasks to be peformed on the relevant skin folders.</summary>
public abstract class SkinMachine : IDisposable
{
    private const int LOG_SPLIT_CHAR_SIZE = 100000;

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

    private readonly List<StringBuilder> _logBuilders = new();

    private StringBuilder _currentLogBuilder;

    private readonly List<Action> _tasks = new();

    private readonly Stopwatch _stopwatch = new();

    private double? _progress;

    private bool _disposedValue;

    public void Run(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        OriginalElementsCache.Clear();
        _tasks.Clear();
        _logBuilders.Clear();

        Settings.Log("Started skin machine.");
        _stopwatch.Reset();
        _stopwatch.Start();

        try
        {
            Progress = 0;

            PopulateTasks();
            RunAllTasks();

            Progress = 100;

            PostRun();

            _stopwatch.Stop();
            Settings.Log($"Finished skin machine in {_stopwatch.Elapsed.TotalSeconds}s");
        }
        catch
        {
            throw;
        }
        finally
        {
            Progress = null;
            Settings.Log("Logs for skin machine follows:");

            _logBuilders.Add(_currentLogBuilder);
            foreach (var builder in _logBuilders)
                Settings.Log(builder.ToString());
        }
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
                    OsuSkinIniSection newSkinSection = workingSkin.SkinIni.Sections.LastOrDefault(s => s.Name == section.Name);
                    if (newSkinSection == null)
                    {
                        newSkinSection = new OsuSkinIniSection(section.Name);
                        workingSkin.SkinIni.Sections.Add(newSkinSection);
                    }

                    AddTask(() =>
                    {
                        Log($"Run task copy skin.ini property '{section.Name}.{pair.Key}: {pair.Value}'");
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

        Log($"Copying skin.ini section '{iniSectionOption.SectionName}' where '{iniSectionOption.Property.Key}: {iniSectionOption.Property.Value}'");

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
            Log($"Run task '{file.FullName}' -> '{destFullPath}' ({reason})");

            using FileStream fileStream = File.Create(destFullPath);
            memoryStream.Position = 0;
            memoryStream.CopyTo(fileStream);

            if (!CacheOriginalElements)
                memoryStream.Dispose();
        });
    }

    protected void AddCopyBlankFileTask(SkinFileOption fileOption, DirectoryInfo fileDestDir)
    {
        string destFullPath = $"{fileDestDir.FullName}/{fileOption.IncludeFileName.Replace("-*", "").Replace("*", "")}.png";

        AddFileToOriginalElementsCache(destFullPath);

        if (fileOption.IsAudio)
        {
            _tasks.Add(() =>
            {
                Log($"Run task (blank file) -> '{destFullPath}'");
                File.Create(destFullPath).Dispose();
            });
        }
        else
        {
            _tasks.Add(() =>
            {
                Log($"Run task (blank file) -> '{destFullPath}'");

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
        MemoryStream originalMemoryStream = null;
        if (File.Exists(fullFilePath))
        {
            FileStream originalFileStream = File.OpenRead(fullFilePath);
            originalMemoryStream = new MemoryStream();
            originalFileStream.CopyTo(originalMemoryStream);
            originalFileStream.Dispose();
        }

        OriginalElementsCache.TryAdd(fullFilePath, originalMemoryStream);
    }

    protected void Log(string message)
    {
        _currentLogBuilder ??= new StringBuilder() { Capacity = LOG_SPLIT_CHAR_SIZE };

        _currentLogBuilder
            .AppendLine()
            .Append(_stopwatch.ElapsedMilliseconds)
            .Append("ms: ")
            .Append(message);

        // Funky stuff happens if we print a massive string, so split it.
        // We don't print the logs as soon as they arrive as we print a lot of logs at once,
        // and Godot flushes the output buffer on program exit, causing a freeze.
        if (_currentLogBuilder.Length > LOG_SPLIT_CHAR_SIZE)
        {
            _logBuilders.Add(_currentLogBuilder);
            _currentLogBuilder = null;
        }
    }

    protected static bool CheckIfFileAndOptionMatch(FileInfo file, SkinFileOption fileOption)
    {
        string filename = Path.GetFileNameWithoutExtension(file.Name);
        string extension = Path.GetExtension(file.Name);

        // Check for file name match.
        if (
            filename.Equals(fileOption.IncludeFileName, StringComparison.OrdinalIgnoreCase) || filename.Equals(fileOption.IncludeFileName + "@2x", StringComparison.OrdinalIgnoreCase)
            || (fileOption.IsAnimatable
                && filename.StartsWith(fileOption.IncludeFileName, StringComparison.OrdinalIgnoreCase)
                && CheckIfFileNameIsAnimation(filename, fileOption.IncludeFileName)
            )
            || (fileOption.IncludeFileName.EndsWith("*")
                && filename.StartsWith(fileOption.IncludeFileName.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)
            )
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

    private static bool CheckIfFileNameIsAnimation(string filename, string fileOptionInclude)
    {
        // An file representing an animation frame would have a number suffix e.g. menu-back-10.png or sliderb10.png.
        string filenameSuffix = filename.ToLower().TrimPrefix(fileOptionInclude.ToLower());
        return int.TryParse(filenameSuffix, out int _) || int.TryParse(filenameSuffix.TrimSuffix("@2x"), out int _);
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
            _logBuilders.Clear();
            _disposedValue = true;
        }
    }
}