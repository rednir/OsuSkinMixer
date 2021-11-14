using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;
using Environment = System.Environment;
using System.Diagnostics;

namespace OsuSkinMixer
{
    public class Main : Control
    {
        public const string VBOX_CONTAINER_PATH = "ScrollContainer/CenterContainer/VBoxContainer";
        public const string WORKING_DIR_NAME = ".osu-skin-mixer_working-skin";

        private Dialog Dialog;
        private Toast Toast;
        private ProgressBar ProgressBar;
        private Label ProgressBarLabel;
        private Button CreateSkinButton;
        private LineEdit SkinNameEdit;
        private ScrollContainer ScrollContainer;

        public override void _Ready()
        {
            OS.SetWindowTitle("osu! skin mixer by rednir");
            OS.MinWindowSize = new Vector2(600, 400);
            Logger.Init();

            Dialog = GetNode<Dialog>("Dialog");
            Toast = GetNode<Toast>("Toast");
            ProgressBar = GetNode<ProgressBar>("ButtonsCenterContainer/HBoxContainer/SkinNameEdit/ProgressBar");
            ProgressBarLabel = GetNode<Label>("ButtonsCenterContainer/HBoxContainer/SkinNameEdit/ProgressBar/Label");
            CreateSkinButton = GetNode<Button>("ButtonsCenterContainer/HBoxContainer/CreateSkinButton");
            SkinNameEdit = GetNode<LineEdit>("ButtonsCenterContainer/HBoxContainer/SkinNameEdit");
            ScrollContainer = GetNode<ScrollContainer>("ScrollContainer");

            GetNode<TopBar>("TopBar").Main = this;

            CreateSkinButton.Connect("pressed", this, nameof(_CreateSkinButtonPressed));

            if (!CreateOptionButtons())
                PromptForSkinsFolder();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("refresh"))
                RefreshSkins();
            else if (@event.IsActionPressed("randomize_suboptions"))
                RandomizeOptions(true);
            else if (@event.IsActionPressed("randomize_options"))
                RandomizeOptions(false);
        }

        private void _SettingsButtonPressed() => PromptForSkinsFolder();

        private void _CreateSkinButtonPressed() => CreateSkin();

        #region Actions

        public void ResetSelections()
        {
            SkinNameEdit.Clear();
            foreach (var option in SkinOptions.Default)
            {
                GetNode<OptionButton>(option.NodePath).Select(0);
                foreach (var suboption in option.SubOptions)
                    GetNode<OptionButton>(suboption.GetPath(option)).Select(0);
            }

            Toast.New("Reset selections!");
        }

        public void RandomizeOptions(bool suboptions)
        {
            var rand = new Random();

            foreach (var option in SkinOptions.Default)
            {
                var optionButton = GetNode<OptionButton>(option.NodePath);
                int count = optionButton.GetItemCount();
                int index = rand.Next(0, count - 1);

                if (suboptions)
                    optionButton.Text = "<< various >>";
                else
                    optionButton?.Select(index);

                foreach (var suboption in option.SubOptions)
                    GetNode<OptionButton>(suboption.GetPath(option)).Select(suboptions ? rand.Next(0, count - 1) : index);
            }

            Toast.New(suboptions ? "Randomized sub-options!" : "Randomized options!");
        }

        public void UseExistingSkin()
        {
            // All the OptionButtons should have equal `Items` anyway, so just get the first.
            var optionButton = GetNodeOrNull<OptionButton>(SkinOptions.Default[0].NodePath);
            if (optionButton == null)
                return;

            Dialog.Options("Select a skin to use.", optionButton.Items, i =>
            {
                // This assumes that index 0 is default skin.
                SkinNameEdit.Text = i == 0 ? string.Empty : optionButton.GetItemText(i);

                foreach (var option in SkinOptions.Default)
                {
                    GetNode<OptionButton>(option.NodePath).Select(i);
                    foreach (var suboption in option.SubOptions)
                        GetNode<OptionButton>(suboption.GetPath(option)).Select(i);
                }
            });
        }

        public void RefreshSkins()
        {
            CreateOptionButtons();
            Toast.New("Refreshed skin list!");
        }

        public void PromptForSkinsFolder()
        {
            Dialog.TextInput(
                text: "Please set the path to your skins folder",
                func: p =>
                {
                    Settings.Content.SkinsFolder = p;
                    Settings.Save();
                    return CreateOptionButtons();
                },
                defaultText: Settings.Content.SkinsFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu!/Skins");
        }

        public void CreateSkin()
        {
            string newSkinName = SkinNameEdit.Text.Replace(']', '-').Replace('[', '-');

            if (string.IsNullOrWhiteSpace(newSkinName))
            {
                Toast.New("Set a name for the new skin first.");
                return;
            }

            if (newSkinName.Any(c => Path.GetInvalidPathChars().Contains(c) || c == '/' || c == '\\'))
            {
                Toast.New("The skin name contains invalid symbols.");
                return;
            }

            if (Directory.Exists(Settings.Content.SkinsFolder + "/" + newSkinName))
            {
                Dialog.Question(
                    text: $"A skin with that name already exists. Replace it?\n\nThis will permanently remove '{newSkinName}'",
                    action: b =>
                    {
                        if (b)
                            runCont();
                    });
                return;
            }

            runCont();

            void runCont()
            {
                CreateSkinButton.Disabled = true;
                ProgressBar.Visible = true;
                ProgressBar.Value = 0;

                Task.Run(cont)
                    .ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            Logger.Log($"Exception thrown:\n\n{t.Exception}\n\n");
                            Dialog.Alert($"Something went wrong.\n\n{t.Exception.Message}");
                        }

                        CreateSkinButton.Disabled = false;
                        ProgressBar.Visible = false;
                    });
            }

            // TODO: split this function up...
            async Task cont()
            {
                Logger.Log($"Beginning skin creation with name '{newSkinName}'");
                ProgressBarLabel.Text = "Preparing...";

                var newSkinIni = new SkinIni(newSkinName, "osu! skin mixer by rednir");
                var newSkinDir = Directory.CreateDirectory($"{Settings.Content.SkinsFolder}/{WORKING_DIR_NAME}");

                // There might be skin elements from a failed attempt still in the directory.
                foreach (var file in newSkinDir.EnumerateFiles())
                    file.Delete();

                foreach (var option in SkinOptions.Default)
                {
                    foreach (var suboption in option.SubOptions)
                    {
                        ProgressBarLabel.Text = $"Copying: {suboption.Name}";

                        var node = GetNode<OptionButton>(suboption.GetPath(option));
                        Logger.Log($"About to copy option {option.Name}/{suboption.Name} set to '{node.Text}'");

                        // User wants default skin elements to be used.
                        if (node.GetSelectedId() == 0)
                            continue;

                        var skindir = new DirectoryInfo(Settings.Content.SkinsFolder + "/" + node.Text);
                        var skinini = new SkinIni(File.ReadAllText(skindir + "/skin.ini"));

                        copySkinIniProperties();
                        copyMatchingFiles();

                        ProgressBar.Value += 100 / SkinOptions.Default.Sum(o => o.SubOptions.Length);

                        void copySkinIniProperties()
                        {
                            foreach (var section in skinini.Sections)
                            {
                                // Only look into ini sections that are specified in the IncludeSkinIniProperties.
                                if (!suboption.IncludeSkinIniProperties.TryGetValue(section.Name, out var includeSkinIniProperties))
                                    continue;

                                bool includeEntireSection = false;
                                if (includeSkinIniProperties.Contains("*"))
                                {
                                    includeEntireSection = true;
                                    newSkinIni.Sections.Add(new SkinIniSection(section.Name));
                                }

                                foreach (var pair in section)
                                {
                                    // Only copy over ini properties that are specified in the IncludeSkinIniProperties.
                                    if (includeSkinIniProperties.Contains(pair.Key) || includeEntireSection)
                                    {
                                        newSkinIni.Sections.Last(s => s.Name == section.Name).Add(
                                            key: pair.Key,
                                            value: pair.Value);

                                        // Check if the skin.ini property value includes any skin elements.
                                        // If so, include it in the new skin, and prioritize their inclusion.
                                        // TODO: only proceed if this skin.ini property is known to have a file path.
                                        int lastSlashIndex = pair.Value.LastIndexOf('/');
                                        string prefixPropertyDirPath = lastSlashIndex >= 0 ? pair.Value.Substring(0, lastSlashIndex) : null;
                                        string prefixPropertyFileName = pair.Value.Substring(lastSlashIndex + 1);

                                        if (Directory.Exists($"{skindir}/{prefixPropertyDirPath}"))
                                        {
                                            var fileDestDir = Directory.CreateDirectory($"{newSkinDir}/{prefixPropertyDirPath}");
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

                        void copyMatchingFiles()
                        {
                            foreach (var file in skindir.EnumerateFiles())
                            {
                                if (File.Exists($"{newSkinDir.FullName}/{file.Name}"))
                                    continue;

                                string filename = Path.GetFileNameWithoutExtension(file.Name);
                                string extension = Path.GetExtension(file.Name);

                                foreach (string optionFilename in suboption.IncludeFileNames)
                                {
                                    // Check for file name match.
                                    if (filename.Equals(optionFilename, StringComparison.OrdinalIgnoreCase) || filename.Equals(optionFilename + "@2x", StringComparison.OrdinalIgnoreCase)
                                        || (optionFilename.EndsWith("*") && filename.StartsWith(optionFilename.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)))
                                    {
                                        // Check for file type match.
                                        if (
                                            ((extension == ".png" || extension == ".jpg") && !suboption.IsAudio)
                                            || ((extension == ".mp3" || extension == ".ogg" || extension == ".wav") && suboption.IsAudio)
                                        )
                                        {
                                            Logger.Log($"'{file.FullName}' -> '{newSkinDir.FullName}/{file.Name}' (due to filename match)");
                                            file.CopyTo($"{newSkinDir.FullName}/{file.Name}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                File.WriteAllText($"{newSkinDir.FullName}/skin.ini", newSkinIni.ToString());

                ProgressBar.Value = 100;
                ProgressBarLabel.Text = "Importing...";

                string dirDestPath = $"{Settings.Content.SkinsFolder}/{newSkinName}";
                Logger.Log($"Copying working folder to '{dirDestPath}'");

                if (Directory.Exists(dirDestPath))
                    Directory.Delete(dirDestPath, true);

                newSkinDir.MoveTo(dirDestPath);

                if (Settings.Content.ImportToGameIfOpen && IsOsuOpen())
                {
                    try
                    {
                        string oskDestPath = $"{Settings.Content.SkinsFolder}/{newSkinName}.osk";
                        Logger.Log($"Importing skin into game from '{oskDestPath}'");

                        // osu! will handle the empty .osk (zip) file by switching the current skin to the skin with name `newSkinName`.
                        File.WriteAllBytes($"{Settings.Content.SkinsFolder}/{newSkinName}.osk", new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                        OS.ShellOpen(oskDestPath);

                        Toast.New("Attempted to import skin into osu!");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Couldn't import skin as an .osk file:\n\n{ex}\n\n");
                        Toast.New("Couldn't import skin as an .osk file.");
                        await Task.Delay(700);
                    }
                }

                Toast.New("Created skin!\nYou might need to press Ctrl+Shift+Alt+S in-game.");
            }
        }

        private bool IsOsuOpen()
        {
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Contains("osu!"))
                    return true;
            }

            return false;
        }

        private string[] GetSkinNames() => new DirectoryInfo(Settings.Content.SkinsFolder).EnumerateDirectories()
            .Select(d => d.Name).Where(n => n != WORKING_DIR_NAME).OrderBy(n => n).ToArray();

        #endregion

        #region Option buttons

        private bool CreateOptionButtons()
        {
            if (Settings.Content.SkinsFolder == null || !Directory.Exists(Settings.Content.SkinsFolder))
                return false;

            var vbox = GetNode(VBOX_CONTAINER_PATH);
            var skins = GetSkinNames();

            foreach (var option in SkinOptions.Default)
            {
                set(GetNodeOrNull<OptionButton>(option.NodePath), option, null);

                foreach (var suboption in option.SubOptions)
                    set(GetNodeOrNull<OptionButton>(suboption.GetPath(option)), option, suboption);
            }

            return true;

            void set(OptionButton optionButton, OptionInfo option, SubOptionInfo suboption)
            {
                bool isSubOption = suboption != null;

                int prevSelectedId = 0;
                string prevText = null;

                if (optionButton == null)
                {
                    var hbox = (HBoxContainer)GetNode("OptionTemplate").Duplicate();
                    var arrowButton = hbox.GetChild<TextureButton>(0);
                    var indent = hbox.GetChild<Panel>(1);
                    var label = hbox.GetChild<Label>(2);
                    optionButton = hbox.GetChild<OptionButton>(3);

                    var binds = new Godot.Collections.Array(new OptionInfoWrapper(option));
                    if (!isSubOption)
                    {
                        optionButton.Connect("item_selected", this, nameof(_OptionItemSelected), binds);
                        arrowButton.Connect("toggled", this, nameof(_ArrowButtonPressed), binds);
                    }
                    else
                    {
                        optionButton.Connect("item_selected", this, nameof(_SubOptionItemSelected), binds);
                    }

                    hbox.Visible = !isSubOption;
                    indent.Visible = isSubOption;
                    arrowButton.Visible = !isSubOption;

                    hbox.Name = suboption?.GetHBoxName(option) ?? option.Name;
                    label.Text = suboption?.Name ?? option.Name;
                    label.Modulate = new Color(1, 1, 1, isSubOption ? 0.7f : 1);

                    vbox.AddChild(hbox);
                }
                else
                {
                    // The existing dropdown items should be updated.
                    prevSelectedId = optionButton.GetSelectedId();
                    prevText = optionButton.Text;
                    optionButton.Clear();
                }

                optionButton.AddItem("<< use default skin >>", 0);
                foreach (var skin in skins)
                    optionButton.AddItem(skin, skin.GetHashCode());

                // If items were updated, ensure the users selection is maintained
                optionButton.Selected = optionButton.GetItemIndex(prevSelectedId);

                // Hotfix for maintaining the previous text when it is << various >>
                if (prevText != null)
                    optionButton.Text = prevText;
            }
        }

        private void _ArrowButtonPressed(bool pressed, OptionInfoWrapper wrapper)
        {
            var option = wrapper.Value;
            foreach (var suboption in option.SubOptions)
                GetNode<HBoxContainer>($"{VBOX_CONTAINER_PATH}/{suboption.GetHBoxName(option)}").Visible = pressed;
        }

        private void _OptionItemSelected(int index, OptionInfoWrapper wrapper)
        {
            var option = wrapper.Value;
            foreach (var suboption in option.SubOptions)
            {
                var node = GetNode<OptionButton>(suboption.GetPath(option));
                node.Select(index);
            }
        }

        private void _SubOptionItemSelected(int _, OptionInfoWrapper wrapper)
        {
            var option = wrapper.Value;
            GetNode<OptionButton>(option.NodePath).Text = "<< various >>";
        }

        // This is required as the `binds` parameter in `Node.Connect()` only takes in a type inherited from `Godot.Object`
        private class OptionInfoWrapper : Godot.Object
        {
            public OptionInfoWrapper(OptionInfo value)
            {
                Value = value;
            }

            public OptionInfo Value { get; }
        }

        #endregion
    }
}