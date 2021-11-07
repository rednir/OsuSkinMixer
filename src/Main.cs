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
        public const string OPTIONS_CONTAINER_PATH = "OptionsContainer/CenterContainer/VBoxContainer";

        private readonly OptionInfo[] Options = new OptionInfo[]
        {
            new OptionInfo
            {
                Name = "Interface",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "Song select",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["Colours"] = new string[]
                            {
                                "MenuGlow",
                                "SongSelectActiveText",
                                "SongSelectInactiveText",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "button-left",
                            "button-middle",
                            "button-right",
                            "menu*",
                            "mode-*",
                            "options-offset-tick",
                            "ranking-a-small",
                            "ranking-b-small",
                            "ranking-c-small",
                            "ranking-d-small",
                            "ranking-s-small",
                            "ranking-sh-small",
                            "ranking-x-small",
                            "ranking-xh-small",
                            "selection-*",
                            "star",
                            "welcome_text",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Results screen",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "ranking-a",
                            "ranking-b",
                            "ranking-c",
                            "ranking-d",
                            "ranking-s",
                            "ranking-sh",
                            "ranking-x",
                            "ranking-xh",
                            "ranking-accuracy",
                            "ranking-graph",
                            "ranking-maxcombo",
                            "ranking-panel",
                            "ranking-perfect",
                            "ranking-replay",
                            "ranking-retry",
                            "ranking-title",
                            "ranking-winner",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "In-game",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["General"] = new string[]
                            {
                                "ComboBurstRandom",
                            },
                            ["Colours"] = new string[]
                            {
                                "InputOverlayText",
                            },
                            ["Fonts"] = new string[]
                            {
                                "ScorePrefix",
                                "ScoreOverlap",
                                "ComboPrefix",
                                "ComboOverlap",
                            }
                        },
                        IncludeFileNames = new string[]
                        {
                            "arrow-pause",
                            "arrow-warning",
                            "count-1",
                            "count-2",
                            "count-3",
                            "fail-background",
                            "go",
                            "inputoverlay*",
                            "pause*",
                            "play*",
                            "scorebar-*",
                            "scoreentry-*",
                            "section-*",
                            "ready",
                            "score-*",
                            // For some reason whitecat 2.1 skin uses this prefix instead of "score-[0-9].png",
                            // this is just a hotfix for that since it's a popular skin.
                            "numbers-*",
                        }
                    },
                },
            },
            new OptionInfo
            {
                Name = "Gameplay",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "osu!",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["General"] = new string[]
                            {
                                "AllowSliderBallTint",
                                "HitCircleOverlayAboveNumber",
                                "SliderBallFlip",
                                "SpinnerFadePlayfield",
                                "SpinnerNoBlink",
                            },
                            ["Colours"] = new string[]
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
                            },
                            ["Fonts"] = new string[]
                            {
                                "HitCirclePrefix",
                                "HitCircleOverlap",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "approachcircle",
                            "comboburst",
                            "default-*",
                            "followpoint*",
                            "hit*",
                            "particle*",
                            "reversearrow",
                            "sliderb*",
                            "sliderendcircle*",
                            "sliderfollowcircle",
                            "sliderpoint*",
                            "sliderstartcircle*",
                            "spinner-*",
                            "target*",
                            "masking-border",    // These two elements are global to all gamemodes, but there's
                            "star-2"             // no real point creating another sub-option just for these.
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Catch the beat",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["CatchTheBeat"] = new string[]
                            {
                                "HyperDash",
                                "HyperDashFruit",
                                "HyperDashAfterImage",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "comboburst-fruits",
                            "fruit-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Taiko",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "taiko*",
                            "pippidon*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Mania",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            // TODO: mania stuff...
                        },
                        IncludeFileNames = new string[]
                        {
                            "mania*",
                            "lighting*"
                        },
                    },
                },
            },
            new OptionInfo
            {
                Name = "Cursor",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "Head",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            {
                                "General", new string[]
                                {
                                    "CursorCentre",
                                    "CursorExpand",
                                    "CursorRotate",
                                }
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "cursor",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Trail",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            {
                                "General", new string[]
                                {
                                    "CursorTrailRotate",
                                }
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "cursortrail",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Smoke",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "cursor-smoke",
                        },
                    },
                },
            },
            new OptionInfo
            {
                Name = "Hitsounds",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "Normal",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "normal-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Soft",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "soft-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Drum",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "drum-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Spinner",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "spinner*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Nightcore beats",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "nightcore-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Combobreak (+)",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["General"] = new string[]
                            {
                                "CustomComboBurstSounds",
                                "LayeredHitSounds",
                                "SpinnerFrequencyModulate",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "combobreak",
                            "comboburst",
                        },
                    },
                },
            },
            new OptionInfo
            {
                Name = "Menu Sounds",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "Interface",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
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
                            "heartbeat",
                            "key*",
                            "match*",
                            "menu*",
                            "metronomelow",
                            "multi-skipped",
                            "seeya",
                            "select-*",
                            "shutter",
                            "sliderbar",
                            "welcome",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "In-game",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "count1s",
                            "count2s",
                            "count3s",
                            "failsound",
                            "gos",
                            "pause*",
                            "readys",
                            "section*",
                        },
                    },
                },
            },
        };

        private class OptionInfo
        {
            public string Name { get; set; }

            public string NodePath => $"{OPTIONS_CONTAINER_PATH}/{Name}/OptionButton";

            public SubOptionInfo[] SubOptions { get; set; }
        }

        private class SubOptionInfo
        {
            public string Name { get; set; }

            public bool IsAudio { get; set; }

            public Dictionary<string, string[]> IncludeSkinIniProperties { get; set; } = new Dictionary<string, string[]>();

            public string[] IncludeFileNames { get; set; }

            public string GetPath(OptionInfo option) => $"{OPTIONS_CONTAINER_PATH}/{GetHBoxName(option)}/OptionButton";

            public string GetHBoxName(OptionInfo option) => $"{option.Name}_{Name}";
        }

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

#region Actions

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
#if DEBUG
                        GD.Print(t.Exception + "\n");
#endif
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
                    foreach (var suboption in option.SubOptions)
                    {
                        var node = GetNode<OptionButton>(suboption.GetPath(option));

                        // User wants default skin elements to be used.
                        if (node.GetSelectedId() == 0)
                            continue;

                        var skindir = new DirectoryInfo(Settings.Content.SkinsFolder + "/" + node.Text);
                        var skinini = new SkinIni(File.ReadAllText(skindir + "/skin.ini"));

                        foreach (var file in skindir.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
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
                                        ((extension == ".png" || extension == ".jpg" || extension == ".ini") && !suboption.IsAudio)
                                        || ((extension == ".mp3" || extension == ".ogg" || extension == ".wav") && suboption.IsAudio)
                                    )
                                    {
                                        if (
                                            // Skin element is in the root of the its skin folder and hasn't already been copied to the new skin.
                                            (file.Directory.FullName == skindir.FullName && !File.Exists(newSkinDir.FullName + "/" + file.Name))

                                            // Skin element is in a sub-directory but the skin.ini mentions it (this is prioritised)
                                            // Ensures unused skin elements in "extra" folders are ignored.
                                            || includeSubdirectoryFile()
                                        )
                                        {
                                            file.CopyTo(newSkinDir.FullName + "/" + file.Name, true);
                                        }

                                        bool includeSubdirectoryFile()
                                        {
                                            // Ignore files that are not in sub-directories.
                                            if (file.Directory.FullName == skindir.FullName)
                                                return false;

                                            // Get the skin.ini property names that contain file paths (file name prefixes) to skin elements.
                                            var skinIniPathProperties = Options
                                                .SelectMany(op => op.SubOptions.SelectMany(so => so.IncludeSkinIniProperties))
                                                .SelectMany(sect => sect.Value.Where(p => p.Contains("Prefix")));

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
                                                    string normalizedPrefix = prefix?.Replace('\\', '/');
                                                    string normalizedPathFromSkinRoot = file.FullName.Substring(skindir.FullName.Length + 1).Replace('\\', '/');

                                                    if ((normalizedPrefix?.Contains('/') ?? false)
                                                        && normalizedPathFromSkinRoot.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
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
                            if (!suboption.IncludeSkinIniProperties.ContainsKey(section.Name))
                                continue;

                            foreach (var pair in section)
                            {
                                // Only copy over ini properties that are specified in the IncludeSkinIniProperties.
                                if (suboption.IncludeSkinIniProperties[section.Name].Contains(pair.Key))
                                {
                                    newSkinIni.Sections.Find(s => s.Name == section.Name).Add(
                                        key: pair.Key,
                                        // All of the skin elements will be in skin directory root, so get rid of child directories in path names.
                                        value: pair.Key.Contains("Prefix") && pair.Value.Contains('/') ? pair.Value.Split('/').Last() : pair.Value);
                                }
                            }
                        }

                        // TODO: ignore options that are set to use default skin.
                        ProgressBar.Value += 100 / Options.Sum(o => o.SubOptions.Length);
                    }
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

#endregion

#region Option buttons

        private bool CreateOptionButtons()
        {
            if (Settings.Content.SkinsFolder == null || !Directory.Exists(Settings.Content.SkinsFolder))
                return false;

            SetWatcher();

            var vbox = GetNode("OptionsContainer/CenterContainer/VBoxContainer");
            var skins = GetSkinNames();

            foreach (var option in Options)
            {
                set(GetNodeOrNull<OptionButton>(option.NodePath), option, null);

                foreach (var suboption in option.SubOptions)
                    set(GetNodeOrNull<OptionButton>(suboption.GetPath(option)), option, suboption);
            }

            return true;

            void set(OptionButton optionButton, OptionInfo option, SubOptionInfo suboption)
            {
                bool isSubOption = suboption != null;
                int selectedId = 0;

                if (optionButton == null)
                {
                    var hbox = (HBoxContainer)GetNode("OptionTemplate").Duplicate();
                    var arrowButton = hbox.GetChild<TextureButton>(0);
                    var indent = hbox.GetChild<Panel>(1);
                    var label = hbox.GetChild<Label>(2);
                    optionButton = hbox.GetChild<OptionButton>(3);

                    if (!isSubOption)
                    {
                        var binds = new Godot.Collections.Array(new OptionInfoWrapper(option));
                        optionButton.Connect("item_selected", this, nameof(_OptionItemSelected), binds);
                        arrowButton.Connect("toggled", this, nameof(_ArrowButtonPressed), binds);
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
        }

        private void _ArrowButtonPressed(bool pressed, OptionInfoWrapper wrapper)
        {
            var option = wrapper.Value;
            foreach (var suboption in option.SubOptions)
                GetNode<HBoxContainer>($"{OPTIONS_CONTAINER_PATH}/{suboption.GetHBoxName(option)}").Visible = pressed;
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

#region File system watcher

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

#endregion

    }
}