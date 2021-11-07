using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;
using Environment = System.Environment;

namespace OsuSkinMixer
{
    public class Main : Control
    {
        private readonly OptionInfo[] Options = new OptionInfo[]
        {
            new OptionInfo
            {
                Name = "Interface",
                IsAudio = false,
                IncludeSkinIniProperties = new Dictionary<string, string[]>()
                {
                    {
                        "General", new string[]
                        {
                            "AllowSliderBallTint",
                            "ComboBurstRandom",
                        }
                    },
                    {
                        "Colours", new string[]
                        {
                            "InputOverlayText",
                            "MenuGlow",
                            "SongSelectActiveText",
                            "SongSelectInactiveText",
                        }
                    },
                    {
                        "Fonts", new string[]
                        {
                            "ScorePrefix",
                            "ScoreOverlap",
                            "ComboPrefix",
                            "ComboOverlap",
                        }
                    },
                },
                IncludeFileNames = new string[]
                {
                    "arrow-pause",
                    "arrow-warning",
                    "button-left",
                    "button-middle",
                    "button-right",
                    "count-1",
                    "count-2",
                    "count-3",
                    "fail-background",
                    "go",
                    "inputoverlay*",
                    "menu*",
                    "mode-*",
                    "options-offset-tick",
                    "pause*",
                    "play*",
                    "ranking*",
                    "ready",
                    "score-*",
                    "scorebar-*",
                    "scoreentry-*",
                    "section-*",
                    "selection-*",
                    "star",
                    "welcome_text",
                    // For some reason whitecat 2.1 skin uses this prefix instead of "score-[0-9].png",
                    // this is just a hotfix for that since it's a popular skin.
                    "numbers-*",
                },
            },
            new OptionInfo
            {
                Name = "Gameplay",
                IsAudio = false,
                IncludeSkinIniProperties = new Dictionary<string, string[]>()
                {
                    {
                        "General", new string[]
                        {
                            "AnimationFramerate",
                            "HitCircleOverlayAboveNumber",
                            "SliderBallFlip",
                            "SpinnerFadePlayfield",
                            "SpinnerNoBlink",
                        }
                    },
                    {
                        "Colours", new string[]
                        {
                            "Combo1",
                            "Combo2",
                            "Combo3",
                            "Combo4",
                            "Combo5",
                            "Combo6",
                            "Combo7",
                            "Combo8",
                            "SliderBall",
                            "SliderBorder",
                            "SliderTrackOverride",
                            "SpinnerBackground",
                            "StarBreakAdditive",
                        }
                    },
                    {
                        "Fonts", new string[]
                        {
                            "HitCirclePrefix",
                            "HitCircleOverlap",
                        }
                    },
                    {
                        "CatchTheBeat", new string[]
                        {
                            "HyperDash",
                            "HyperDashFruit",
                            "HyperDashAfterImage",
                        }
                    },
                },
                IncludeFileNames = new string[]
                {
                    "approachcircle",
                    "comboburst*",
                    "default-*",
                    "followpoint*",
                    "fruit-*",
                    "hit*",
                    "lighting*",
                    "mania*",
                    "masking-border",
                    "particle*",
                    "reversearrow",
                    "sliderb*",
                    "sliderendcircle*",
                    "sliderfollowcircle",
                    "sliderpoint*",
                    "sliderstartcircle*",
                    "spinner-*",
                    "star2",
                    "taiko*",
                    "target*",
                    "skin",
                },
            },
            new OptionInfo
            {
                Name = "Cursor",
                IsAudio = false,
                IncludeSkinIniProperties = new Dictionary<string, string[]>()
                {
                    {
                        "General", new string[]
                        {
                            "CursorCentre",
                            "CursorExpand",
                            "CursorRotate",
                            "CursorTrailRotate",
                        }
                    },
                },
                IncludeFileNames = new string[]
                {
                    "cursor*",
                }
            },
            new OptionInfo
            {
                Name = "Hitsounds",
                IsAudio = true,
                IncludeSkinIniProperties = new Dictionary<string, string[]>()
                {
                    {
                        "General", new string[]
                        {
                            "CustomComboBurstSounds",
                            "LayeredHitSounds",
                            "SpinnerFrequencyModulate",
                        }
                    },
                },
                IncludeFileNames = new string[]
                {
                    "combobreak",
                    "comboburst",
                    "drum-*",
                    "nightcore-*",
                    "normal-*",
                    "pippidon*",
                    "soft-*",
                    "spinner-*",
                },
            },
            new OptionInfo
            {
                Name = "Menu Sounds",
                IsAudio = true,
                IncludeFileNames = new string[]
                {
                    "applause",
                    "back-button-click",
                    "back-button-hover",
                    "check-off",
                    "check-on",
                    "click-close",
                    "click-short-confirm",
                    "click-short",
                    "count1s",
                    "count2s",
                    "count3s",
                    "failsound",
                    "gos",
                    "heartbeat",
                    "key*",
                    "match*",
                    "menu*",
                    "metronomelow",
                    "multi-skipped",
                    "pause*",
                    "readys",
                    "section*",
                    "seeya",
                    "select-*",
                    "shutter",
                    "sliderbar",
                    "welcome",
                },
            },
        };

        private Dialog Dialog;
        private Toast Toast;
        private ProgressBar ProgressBar;
        private Button CreateSkinButton;
        private LineEdit SkinNameEdit;

        private FileSystemWatcher FileSystemWatcher { get; set; }

        public override void _Ready()
        {
            OS.SetWindowTitle("osu! skin mixer");

            Dialog = GetNode<Dialog>("Dialog");
            Toast = GetNode<Toast>("Toast");
            ProgressBar = GetNode<ProgressBar>("ButtonsCenterContainer/HBoxContainer/SkinNameEdit/ProgressBar");
            CreateSkinButton = GetNode<Button>("ButtonsCenterContainer/HBoxContainer/CreateSkinButton");
            SkinNameEdit = GetNode<LineEdit>("ButtonsCenterContainer/HBoxContainer/SkinNameEdit");

            GetNode<TopBar>("TopBar").Main = this;

            CreateSkinButton.Connect("pressed", this, "_CreateSkinButtonPressed");

            if (!CreateOptionButtons())
                PromptForSkinsFolder();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("refresh"))
                RefreshSkins();
        }

        private void _SettingsButtonPressed() => PromptForSkinsFolder();

        private void _CreateSkinButtonPressed() => CreateSkin();

        public void ResetSelections()
        {
            SkinNameEdit.Clear();
            foreach (var option in Options)
            {
                var optionButton = GetNodeOrNull<OptionButton>(option.NodePath);
                optionButton?.Select(0);
            }

            Toast.New("Reset selections!");
        }

        public void RandomizeSelections()
        {
            var rand = new Random();
            foreach (var option in Options)
            {
                var optionButton = GetNodeOrNull<OptionButton>(option.NodePath);
                optionButton?.Select(rand.Next(0, optionButton.GetItemCount() - 1));
            }

            Toast.New("Randomized selections!");
        }

        public void UseExistingSkin()
        {
            // All the OptionButtons should have equal `Items` anyway, so just get the first.
            var optionButton = GetNodeOrNull<OptionButton>(Options[0].NodePath);
            if (optionButton == null)
                return;

            Dialog.Options("Select a skin to use.", optionButton.Items, i =>
            {
                // This assumes that index 0 is default skin.
                SkinNameEdit.Text = i == 0 ? string.Empty : optionButton.GetItemText(i);

                foreach (var option in Options)
                {
                    var node = GetNodeOrNull<OptionButton>(option.NodePath);
                    node?.Select(i);
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
                Dialog.Alert("Set a name for the new skin first.");
                return;
            }

            if (newSkinName.Any(c => Path.GetInvalidPathChars().Contains(c) || c == '/' || c == '\\'))
            {
                Dialog.Alert("The skin name contains invalid symbols.");
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
                CreateSkinButton.Text = "Working...";

                ProgressBar.Visible = true;
                ProgressBar.Value = 0;

                Task.Run(cont)
                    .ContinueWith(t =>
                    {
                        if (t.Exception != null)
                            Dialog.Alert($"Something went wrong.\n\n{t.Exception.Message}");

                        CreateSkinButton.Disabled = false;
                        CreateSkinButton.Text = "Create skin";

                        ProgressBar.Visible = false;
                    });
            }

            void cont()
            {
                var newSkinIni = new SkinIni(newSkinName, "osu! skin mixer by rednir");
                var newSkinDir = Directory.CreateDirectory(Settings.Content.SkinsFolder + "/.osu-skin-mixer_working-skin");

                // There might be skin elements from a failed attempt still in the directory.
                foreach (var file in newSkinDir.EnumerateFiles())
                    file.Delete();

                foreach (var option in Options)
                {
                    var node = GetNode<OptionButton>(option.NodePath);

                    // User wants default skin elements to be used.
                    if (node.GetSelectedId() == 0)
                        continue;

                    var skindir = new DirectoryInfo(Settings.Content.SkinsFolder + "/" + node.Text);
                    var skinini = new SkinIni(File.ReadAllText(skindir + "/skin.ini"));

                    foreach (var file in skindir.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        string filename = Path.GetFileNameWithoutExtension(file.Name);
                        string extension = Path.GetExtension(file.Name);

                        foreach (string optionFilename in option.IncludeFileNames)
                        {
                            // Check for file name match.
                            if (filename.Equals(optionFilename, StringComparison.OrdinalIgnoreCase) || filename.Equals(optionFilename + "@2x", StringComparison.OrdinalIgnoreCase)
                                || (optionFilename.EndsWith("*") && filename.StartsWith(optionFilename.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)))
                            {
                                // Check for file type match.
                                if (
                                    ((extension == ".png" || extension == ".jpg" || extension == ".ini") && !option.IsAudio)
                                    || ((extension == ".mp3" || extension == ".ogg" || extension == ".wav") && option.IsAudio)
                                )
                                {
                                    if (file.Directory.FullName != skindir.FullName && isFileIncludedInSkinIni())
                                        file.CopyTo(newSkinDir.FullName + "/" + file.Name, true);
                                    else if (file.Directory.FullName == skindir.FullName && !File.Exists(newSkinDir.FullName + "/" + file.Name))
                                        file.CopyTo(newSkinDir.FullName + "/" + file.Name, false);

                                    // Make sure skin elements that are not used are ignored (for example, skin elements in "extra" folders)
                                    bool isFileIncludedInSkinIni()
                                    {
                                        // Get the skin.ini property names that contain file paths (file name prefixes) to skin elements.
                                        var skinIniPathProperties = Options.SelectMany(
                                            op => op.IncludeSkinIniProperties.SelectMany(
                                                sect => sect.Value.Where(p => p.Contains("Prefix"))));

                                        foreach (string propName in skinIniPathProperties)
                                        {
                                            // Get the file path "prefixes" that skin elements should be taken from.
                                            var prefixes = skinini.Sections.Select(s =>
                                            {
                                                s.TryGetValue(propName, out string result);
                                                return result;
                                            });

                                            foreach (string prefix in prefixes)
                                            {
                                                if (prefix != null && file.FullName.Substring(skindir.FullName.Length + 1).Replace('\\', '/')
                                                    .StartsWith(prefix.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                                                {
                                                    return true;
                                                }
                                            }
                                        }

                                        return false;
                                    }
                                }
                            }
                        }
                    }

                    foreach (var section in skinini.Sections)
                    {
                        // Only look into ini sections that are specified in the IncludeSkinIniProperties.
                        if (!option.IncludeSkinIniProperties.ContainsKey(section.Name))
                            continue;

                        foreach (var pair in section)
                        {
                            // Only copy over ini properties that are specified in the IncludeSkinIniProperties.
                            if (option.IncludeSkinIniProperties[section.Name].Contains(pair.Key))
                            {
                                newSkinIni.Sections.Find(s => s.Name == section.Name).Add(
                                    key: pair.Key,
                                    // All of the skin elements will be in skin directory root, so get rid of child directories in path names.
                                    value: pair.Key.Contains("Prefix") && pair.Value.Contains('/') ? pair.Value.Split('/').Last() : pair.Value);
                            }
                        }
                    }

                    ProgressBar.Value += 100 / Options.Length;
                }

                File.WriteAllText(newSkinDir.FullName + "/skin.ini", newSkinIni.ToString());

                string destPath = Settings.Content.SkinsFolder + "/" + newSkinName;
                if (Directory.Exists(destPath))
                    Directory.Delete(destPath, true);

                newSkinDir.MoveTo(destPath);

                Dialog.Alert($"Created skin '{newSkinName}'.\n\nYou might need to press Ctrl+Shift+Alt+S in-game for the skin to appear.");
            }
        }

        private string[] GetSkinNames() => new DirectoryInfo(Settings.Content.SkinsFolder).EnumerateDirectories().Select(d => d.Name).OrderBy(n => n).ToArray();

        private bool CreateOptionButtons()
        {
            if (Settings.Content.SkinsFolder == null || !Directory.Exists(Settings.Content.SkinsFolder))
                return false;

            SetWatcher();

            var vbox = GetNode("OptionsContainer/CenterContainer/VBoxContainer");
            var skins = GetSkinNames();

            foreach (var option in Options)
            {
                var optionButton = GetNodeOrNull<OptionButton>(option.NodePath);
                int selectedId = 0;

                if (optionButton == null)
                {
                    var hbox = GetNode<HBoxContainer>("OptionTemplate").Duplicate();
                    var label = hbox.GetChild<Label>(0);
                    optionButton = hbox.GetChild<OptionButton>(1);

                    hbox.Name = option.Name;
                    label.Text = option.Name;

                    vbox.AddChild(hbox);
                }
                else
                {
                    // The existing dropdown items should be updated.
                    selectedId = optionButton.GetSelectedId();
                    optionButton.Clear();
                }

                optionButton.AddItem("<< use default skin >>", 0);
                optionButton.AddSeparator();
                foreach (var skin in skins)
                    optionButton.AddItem(skin, skin.GetHashCode());

                // If items were updated, ensure the users selection is maintained
                optionButton.Selected = optionButton.GetItemIndex(selectedId);
            }

            return true;
        }

        private void SetWatcher()
        {
            if (FileSystemWatcher != null)
                return;

            FileSystemWatcher = new FileSystemWatcher(Settings.Content.SkinsFolder)
            {
                NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
            };

            FileSystemWatcher.Changed += OnWatcherEvent;
            FileSystemWatcher.Created += OnWatcherEvent;
            FileSystemWatcher.Deleted += OnWatcherEvent;
        }

        private void OnWatcherEvent(object sender, FileSystemEventArgs e)
        {
            Toast.New("Change in skin folder detected\nPress F5 to refresh skins!");
        }

        private class OptionInfo
        {
            public string Name { get; set; }

            public string NodePath => $"OptionsContainer/CenterContainer/VBoxContainer/{Name}/OptionButton";

            public bool IsAudio { get; set; }

            public Dictionary<string, string[]> IncludeSkinIniProperties { get; set; } = new Dictionary<string, string[]>();

            public string[] IncludeFileNames { get; set; }
        }
    }
}