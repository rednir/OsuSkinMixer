namespace OsuSkinMixer.Utils;

using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OsuSkinMixer.Models;
using OsuSkinMixer.src.Models.Osu;
using OsuSkinMixer.Statics;

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

    public Action<string> StatusChanged { get; set; }

    protected virtual bool CacheOriginalElements => false;

    protected Dictionary<string, MemoryStream> OriginalElementsCache { get; } = new();

    protected Dictionary<SkinFileOption, (string filename, string checksum)> Md5Map { get; } = new();

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

            Settings.Content.SkinsMadeCount++;
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
        StatusChanged?.Invoke("Writing changes...");
        double progressInterval = (100.0 - Progress.Value) / _tasks.Count;
        foreach (Action task in _tasks)
        {
            Progress += progressInterval;
            task();
        }
    }

    protected abstract void PostRun();

    protected void GenerateCreditsFile(OsuSkin workingSkin)
    {
        string creditsFilePath = $"{workingSkin.Directory.FullName}/credits.ini";

        foreach (var pair in Md5Map)
        {
            OsuSkin skin = pair.Key.Value.CustomSkin;

            // We use this instead of SkinFileOption.IncludeFileName because these can have wildcards (e.g. default-*) representing more than one file.
            string elementFilename = pair.Value.filename;

            if (skin.Credits.TryGetSkinFromElementFilename(elementFilename, out OsuSkinCreditsSkin skinToCredit))
            {
                workingSkin.Credits.AddElement(
                    skinName: skinToCredit.SkinName,
                    skinAuthor: skinToCredit.SkinAuthor,
                    checksum: pair.Value.checksum,
                    filename: elementFilename);

                continue;
            }

            workingSkin.Credits.AddElement(
                skinName: skin.Name,
                skinAuthor: skin.SkinIni?.TryGetPropertyValue("General", "Author"),
                checksum: pair.Value.checksum,
                filename: elementFilename);
        }

        File.WriteAllText(creditsFilePath, workingSkin.Credits.ToString());
    }

    private static string GetMd5Hash(string filePath)
    {
        using MD5 md5 = MD5.Create();
        using FileStream stream = File.OpenRead(filePath);

        byte[] hashBytes = md5.ComputeHash(stream);

        StringBuilder sb = new();
        foreach (byte b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }

    protected void CopyOption(OsuSkin workingSkin, SkinOption option)
    {
        StatusChanged?.Invoke($"Copying: {(option as SkinFileOption)?.IncludeFileName ?? option.Name}");
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
            {
                AddCopyFileTask(file, workingSkin.Directory, "due to filename match");
                Md5Map[fileOption] = (Path.GetFileNameWithoutExtension(file.Name), GetMd5Hash(file.FullName));
            }
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
        if (fileOption.IncludeFileName.EndsWith("*"))
        {
            string filenameWithoutWildcard = fileOption.IncludeFileName.Replace("*", string.Empty);

            if (fileOption.AllowedSuffixes == null)
            {
                add(filenameWithoutWildcard);
                return;
            }

            foreach (var suffix in fileOption.AllowedSuffixes)
                add($"{filenameWithoutWildcard}{suffix}");

            return;
        }

        add(fileOption.IncludeFileName);

        void add(string filename)
        {
            string destPathWithoutExtension = Path.Combine(fileDestDir.FullName, filename);

            if (fileOption.IsAudio)
            {
                string wavDestPath = $"{destPathWithoutExtension}.wav";
                AddFileToOriginalElementsCache(wavDestPath);
                _tasks.Add(() =>
                {
                    Log($"Run task (blank file) -> '{wavDestPath}'");
                    File.Create(wavDestPath).Dispose();
                });
            }
            else
            {
                string pngDestPath = $"{destPathWithoutExtension}.png";
                string pngDestPath2x = $"{destPathWithoutExtension}@2x.png";

                AddFileToOriginalElementsCache(pngDestPath);
                AddFileToOriginalElementsCache(pngDestPath2x);

                _tasks.Add(() =>
                {
                    Log($"Run task (blank file) -> '{pngDestPath}'");

                    // This is a 1x1 transparent PNG file. A zero byte file will cause osu! to fall back to the default skin.
                    File.WriteAllBytes(pngDestPath, TransparentPngFile);
                    File.WriteAllBytes(pngDestPath2x, TransparentPngFile);
                });
            }
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
            filename.Equals(fileOption.IncludeFileName, StringComparison.OrdinalIgnoreCase)
            || filename.Equals(fileOption.IncludeFileName + "@2x", StringComparison.OrdinalIgnoreCase)
            || (
                filename.StartsWith(fileOption.IncludeFileName.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)
                && (
                    CheckIfFileNameIsAnimation(filename, fileOption)
                    || CheckIfWildcardMatchesFile(filename, fileOption)
                )
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

    private static bool CheckIfFileNameIsAnimation(string filename, SkinFileOption fileOption)
    {
        if (!fileOption.IsAnimatable)
            return false;

        // An file representing an animation frame would have a number suffix e.g. menu-back-10.png or sliderb10.png.
        string filenameSuffix = filename.ToLower().TrimPrefix(fileOption.IncludeFileName.ToLower());
        return int.TryParse(filenameSuffix, out int _) || int.TryParse(filenameSuffix.TrimSuffix("@2x"), out int _);
    }

    private static bool CheckIfWildcardMatchesFile(string filename, SkinFileOption fileOption)
    {
        if (!fileOption.IncludeFileName.EndsWith("*"))
            return false;

        if (fileOption.AllowedSuffixes == null)
            return true;

        string filenameSuffix = filename.ToLower().TrimSuffix("@2x").TrimPrefix(fileOption.IncludeFileName.ToLower().TrimEnd('*'));
        return fileOption.AllowedSuffixes.Any(s => filenameSuffix.Equals(s, StringComparison.OrdinalIgnoreCase));
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