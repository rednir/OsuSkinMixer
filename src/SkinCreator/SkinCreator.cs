using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace OsuSkinMixer
{
    public class SkinCreator
    {
        public const string WORKING_DIR_NAME = ".osu-skin-mixer_working-skin";

        public string Name { get; set; }

        public SkinOption[] SkinOptions { get; set; }

        public Skin[] Skins { get; set; }

        public float Progress { get; private set; }

        public string Status { get; set; }

        public bool InProgress { get; set; }

        private readonly List<SkinWithFiles> CachedSkinWithFiles = new List<SkinWithFiles>();
        private SkinIni NewSkinIni;
        private DirectoryInfo NewSkinDir;

        public void Create(bool overwrite, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new SkinCreationInvalidException("Set a name for the new skin first.");

            if (Name.Any(c => Path.GetInvalidPathChars().Contains(c) || c == '/' || c == '\\'))
                throw new SkinCreationInvalidException("The skin name contains invalid symbols.");

            if (Directory.Exists(Settings.Content.SkinsFolder + "/" + Name) && !overwrite)
                throw new SkinExistsException();

            try
            {
                InProgress = true;
                CreateSkin(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SkinCreationFailedException($"Skin creation failure on '{Status}'", ex);
            }
            finally
            {
                CachedSkinWithFiles.Clear();
                InProgress = false;
                NewSkinIni = null;
                NewSkinDir = null;
            }
        }

        public void TriggerOskImport()
        {
            string oskDestPath = $"{Settings.Content.SkinsFolder}/{Name}.osk";
            Logger.Log($"Importing skin into game from '{oskDestPath}'");

            // osu! will handle the empty .osk (zip) file by switching the current skin to the skin with name `newSkinName`.
            File.WriteAllBytes(oskDestPath, new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            Godot.OS.ShellOpen(oskDestPath);
        }

        private void CreateSkin(CancellationToken cancellationToken)
        {
            Logger.Log($"Beginning skin creation with name '{Name}'");

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
            float progressInterval = 100f / flattenedOptions.Count(o => o.OptionButton.Selected > 0);

            foreach (var option in flattenedOptions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.Log($"About to copy option '{option.Name}' set to '{option.OptionButton.Text}'");
                Status = $"Copying: {option.Name}";

                // User wants default skin elements to be used.
                if (option.OptionButton.GetSelectedId() == 0)
                    continue;

                CopyOption(option);
                Progress += progressInterval;
            }

            File.WriteAllText($"{NewSkinDir.FullName}/skin.ini", NewSkinIni.ToString());

            Progress = 100;
            Status = "Importing...";

            string dirDestPath = $"{Settings.Content.SkinsFolder}/{Name}";
            Logger.Log($"Copying working folder to '{dirDestPath}'");

            if (Directory.Exists(dirDestPath))
                Directory.Delete(dirDestPath, true);

            NewSkinDir.MoveTo(dirDestPath);

            Logger.Log($"Skin creation for '{Name}' has completed.");
        }

        private void CopyOption(SkinOption option)
        {
            var skin = GetSkinWithFiles(option.OptionButton.Text);

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

            Logger.Log($"Copying skin.ini section '{iniSectionOption.SectionName}' where '{iniSectionOption.Property.Key}: {iniSectionOption.Property.Value}'");

            NewSkinIni.Sections.Add(section);
            foreach (var property in section)
                CopyFileFromSkinIniProperty(skin, property);
        }

        private void CopyFileFromSkinIniProperty(SkinWithFiles skin, KeyValuePair<string, string> property)
        {
            if (!SkinIni.PropertyHasFilePath(property.Key))
                return;

            int lastSlashIndex = property.Value.LastIndexOf('/');
            string prefixPropertyDirPath = lastSlashIndex >= 0 ? property.Value.Substring(0, lastSlashIndex) : null;
            string prefixPropertyFileName = property.Value.Substring(lastSlashIndex + 1);

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
                    Logger.Log($"'{file.FullName}' -> '{fileDestDir.FullName}' (due to skin.ini)");
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
                        Logger.Log($"'{file.FullName}' -> '{NewSkinDir.FullName}/{file.Name}' (due to filename match)");

                        try
                        {
                            file.CopyTo($"{NewSkinDir.FullName}/{file.Name}");
                        }
                        catch (IOException)
                        {
                            Logger.Log("...but it failed, probably because the file already exists.");
                        }
                    }
                }
            }
        }

        private SkinWithFiles GetSkinWithFiles(string name)
        {
            var existing = CachedSkinWithFiles.Find(s => s.Name == name);
            if (existing != null)
                return existing;

            var skin = Array.Find(Skins, s => s.Name == name);
            if (skin == null)
                throw new SkinCreationFailedException($"Skin '{name}' does not exist. Try F5.");

            Logger.Log($"Caching skin's files for '{skin.Name}'");
            var skinWithFiles = new SkinWithFiles(skin);
            CachedSkinWithFiles.Add(skinWithFiles);

            return skinWithFiles;
        }
    }
}
