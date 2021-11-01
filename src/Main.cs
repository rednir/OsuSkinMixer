using Godot;
using System;
using System.IO;
using System.Linq;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;

namespace OsuSkinMixer
{
    public class Main : Control
    {
        private readonly OptionInfo[] Options = new OptionInfo[]
        {
            new OptionInfo
            {
                ContainerNodeName = "Interface",
                IsAudio = false,
                IncludeSkinIniProperties = new string[]
                {
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
                },
            },
            new OptionInfo
            {
                ContainerNodeName = "Gameplay",
                IsAudio = false,
                IncludeSkinIniProperties = new string[]
                {
                },
                IncludeFileNames = new string[]
                {
                    "approachcircle",
                    "comboburst*",
                    "cursor-ripple",
                    "cursor-smoke",
                    "cursor",
                    "cursor-middle",
                    "cursor-trail",
                    "cursor-trail",
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
                ContainerNodeName = "Hitsounds",
                IsAudio = true,
                IncludeSkinIniProperties = new string[]
                {
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
                ContainerNodeName = "MenuSounds",
                IsAudio = true,
                IncludeSkinIniProperties = new string[]
                {
                },
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
        private Button CreateSkinButton;
        private LineEdit SkinNameEdit;

        private string SkinsFolder { get; set; } = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + "/osu!/Skins";

        private string[] Skins { get; set; } = Array.Empty<string>();

        public override void _Ready()
        {
            OS.SetWindowTitle("osu! skin mixer");

            Dialog = GetNode<Dialog>("Dialog");
            CreateSkinButton = GetNode<Button>("ButtonsCenterContainer/HBoxContainer/CreateSkinButton");
            SkinNameEdit = GetNode<LineEdit>("ButtonsCenterContainer/HBoxContainer/SkinNameEdit");

            CreateSkinButton.Connect("pressed", this, "_CreateSkinButtonPressed");

            if (!LoadSkins())
            {
                Dialog.TextInput(
                    text: "Couldn't find your skins folder, please set it below.",
                    func: p =>
                    {
                        SkinsFolder = p;
                        return LoadSkins();
                    },
                    defaultText: SkinsFolder);
            }
        }

        public void _CreateSkinButtonPressed()
        {
            string skinName = SkinNameEdit.Text;

            if (string.IsNullOrWhiteSpace(skinName))
            {
                Dialog.Alert("Set a name for the new skin first.");
                return;
            }

            if (Directory.Exists(SkinsFolder + "/" + skinName))
            {
                Dialog.Alert("A skin with that name already exists.");
                return;
            }

            var newSkinIni = new SkinIni();
            newSkinIni.Sections.Add(new SkinIniSection("General")
            {
                { "Name", skinName },
                { "Author", "osu! skin mixer by rednir" },
                { "Version", "1.0" },
            });

            var newSkinDir = Directory.CreateDirectory(SkinsFolder + "/" + skinName);
            File.WriteAllText(newSkinDir.FullName + "/skin.ini", newSkinIni.ToString());

            foreach (var option in Options)
            {
                var node = GetNode<OptionButton>(option.NodePath);

                // User wants default skin elements to be used.
                if (node.GetSelectedId() == 0)
                    continue;

                var skindir = new DirectoryInfo(SkinsFolder + "/" + node.Text);
                var skinini = new SkinIni(File.ReadAllText(skindir + "/skin.ini"));

                foreach (var file in skindir.EnumerateFiles())
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
                                file.CopyTo(newSkinDir.FullName);
                            }
                        }
                    }
                }
            }

            Dialog.Alert("Created skin.\n\nYou might need to press Ctrl+Shift+Alt+S in-game for the skin to appear.");
        }

        private bool LoadSkins()
        {
            if (!Directory.Exists(SkinsFolder))
                return false;

            var directory = new DirectoryInfo(SkinsFolder);
            Skins = directory.EnumerateDirectories().Select(d => d.Name).ToArray();

            foreach (var option in Options)
            {
                var node = GetNode<OptionButton>(option.NodePath);
                node.Clear();
                node.AddItem("<< use default skin >>");
                node.AddSeparator();
                foreach (var skin in Skins)
                    node.AddItem(skin);
            }

            return true;
        }

        private class OptionInfo
        {
            public string ContainerNodeName { get; set; }

            public string NodePath => $"OptionsContainer/{ContainerNodeName}/OptionButton";

            public bool IsAudio { get; set; }

            public string[] IncludeSkinIniProperties { get; set; }

            public string[] IncludeFileNames { get; set; }
        }
    }
}