using System;
using System.IO;
using System.Linq;

namespace OsuSkinMixer
{
    public class SkinCreator
    {
        public const string WORKING_DIR_NAME = ".osu-skin-mixer_working-skin";

        public string Name { get; set; }

        public OptionInfo[] Options { get; set; }

        public Action<int, string> ProgressSetter { get; set; }

        public int Progress { get; private set; }

        private OptionInfo CurrentOption;
        private SubOptionInfo CurrentSubOption;
        private SkinIni NewSkinIni;
        private DirectoryInfo NewSkinDir;

        public void Create(bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Set a name for the new skin first.");

            if (Name.Any(c => Path.GetInvalidPathChars().Contains(c) || c == '/' || c == '\\'))
                throw new InvalidOperationException("The skin name contains invalid symbols.");

            if (Directory.Exists(Settings.Content.SkinsFolder + "/" + Name) && !overwrite)
                throw new SkinExistsException();

            Logger.Log($"Beginning skin creation with name '{Name}'");

            Progress = 0;
            ProgressSetter?.Invoke(Progress, "Preparing...");

            NewSkinIni = new SkinIni(Name, "osu! skin mixer by rednir");
            NewSkinDir = Directory.CreateDirectory($"{Settings.Content.SkinsFolder}/{WORKING_DIR_NAME}");

            // There might be skin elements from a failed attempt still in the directory.
            foreach (var file in NewSkinDir.EnumerateFiles())
                file.Delete();

            foreach (var option in Options)
            {
                foreach (var suboption in option.SubOptions)
                {
                    CurrentOption = option;
                    CurrentSubOption = suboption;
                    CopySubOption();
                }
            }

            File.WriteAllText($"{NewSkinDir.FullName}/skin.ini", NewSkinIni.ToString());

            ProgressSetter?.Invoke(100, "Importing...");

            string dirDestPath = $"{Settings.Content.SkinsFolder}/{Name}";
            Logger.Log($"Copying working folder to '{dirDestPath}'");

            if (Directory.Exists(dirDestPath))
                Directory.Delete(dirDestPath, true);

            NewSkinDir.MoveTo(dirDestPath);

            Logger.Log($"Skin creation for '{Name}' has completed.");
        }

        public void TriggerOskImport()
        {
            string oskDestPath = $"{Settings.Content.SkinsFolder}/{Name}.osk";
            Logger.Log($"Importing skin into game from '{oskDestPath}'");

            // osu! will handle the empty .osk (zip) file by switching the current skin to the skin with name `newSkinName`.
            File.WriteAllBytes(oskDestPath, new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            Godot.OS.ShellOpen(oskDestPath);
        }

        private void CopySubOption()
        {
            ProgressSetter?.Invoke(Progress, $"Copying: {CurrentSubOption.Name}");
            Logger.Log($"About to copy option {CurrentOption.Name}/{CurrentSubOption.Name} set to '{CurrentSubOption.OptionButton.Text}'");

            // User wants default skin elements to be used.
            if (CurrentSubOption.OptionButton.GetSelectedId() == 0)
                return;

            var skindir = new DirectoryInfo($"{Settings.Content.SkinsFolder}/{CurrentSubOption.OptionButton.Text}");

            CopySkinIni(skindir);
            CopySkinMatchingFiles(skindir);

            Progress += 100 / SkinOptions.Default.Sum(o => o.SubOptions.Length);
        }

        private void CopySkinIni(DirectoryInfo skindir)
        {
            var skinini = new SkinIni(File.ReadAllText(skindir + "/skin.ini"));

            foreach (var section in skinini.Sections)
            {
                // Only look into ini sections that are specified in the IncludeSkinIniProperties.
                if (!CurrentSubOption.IncludeSkinIniProperties.TryGetValue(section.Name, out var includeSkinIniProperties))
                    continue;

                bool includeEntireSection = false;
                if (includeSkinIniProperties.Contains("*"))
                {
                    includeEntireSection = true;
                    NewSkinIni.Sections.Add(new SkinIniSection(section.Name));
                }

                foreach (var pair in section)
                {
                    // Only copy over ini properties that are specified in the IncludeSkinIniProperties.
                    if (includeSkinIniProperties.Contains(pair.Key) || includeEntireSection)
                    {
                        NewSkinIni.Sections.Last(s => s.Name == section.Name).Add(
                            key: pair.Key,
                            value: pair.Value);

                        // Check if the skin.ini property value includes any skin elements.
                        // If so, include it in the new skin, (their inclusion takes priority over the elements from matching filenames)
                        // TODO: we only need to proceed if this skin.ini property is known to have a file path.
                        int lastSlashIndex = pair.Value.LastIndexOf('/');
                        string prefixPropertyDirPath = lastSlashIndex >= 0 ? pair.Value.Substring(0, lastSlashIndex) : null;
                        string prefixPropertyFileName = pair.Value.Substring(lastSlashIndex + 1);

                        if (Directory.Exists($"{skindir}/{prefixPropertyDirPath}"))
                        {
                            var fileDestDir = Directory.CreateDirectory($"{NewSkinDir}/{prefixPropertyDirPath}");
                            foreach (var file in new DirectoryInfo($"{skindir}/{prefixPropertyDirPath}").EnumerateFiles())
                            {
                                if (file.Name.StartsWith(prefixPropertyFileName, StringComparison.OrdinalIgnoreCase))
                                {
                                    Logger.Log($"'{file.FullName}' -> '{fileDestDir.FullName}' (due to skin.ini)");
                                    file.CopyTo($"{fileDestDir.FullName}/{file.Name}", true);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CopySkinMatchingFiles(DirectoryInfo skindir)
        {
            foreach (var file in skindir.EnumerateFiles())
            {
                if (File.Exists($"{NewSkinDir.FullName}/{file.Name}"))
                    continue;

                string filename = Path.GetFileNameWithoutExtension(file.Name);
                string extension = Path.GetExtension(file.Name);

                foreach (string optionFilename in CurrentSubOption.IncludeFileNames)
                {
                    // Check for file name match.
                    if (filename.Equals(optionFilename, StringComparison.OrdinalIgnoreCase) || filename.Equals(optionFilename + "@2x", StringComparison.OrdinalIgnoreCase)
                        || (optionFilename.EndsWith("*") && filename.StartsWith(optionFilename.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)))
                    {
                        // Check for file type match.
                        if (
                            ((extension == ".png" || extension == ".jpg") && !CurrentSubOption.IsAudio)
                            || ((extension == ".mp3" || extension == ".ogg" || extension == ".wav") && CurrentSubOption.IsAudio)
                        )
                        {
                            Logger.Log($"'{file.FullName}' -> '{NewSkinDir.FullName}/{file.Name}' (due to filename match)");
                            file.CopyTo($"{NewSkinDir.FullName}/{file.Name}");
                        }
                    }
                }
            }
        }
    }
}