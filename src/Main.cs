using Godot;
using System;
using System.IO;
using System.Linq;
using Directory = System.IO.Directory;
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
            },
            new OptionInfo
            {
                ContainerNodeName = "Gameplay",
            },
            new OptionInfo
            {
                ContainerNodeName = "Hitsounds",
            },
            new OptionInfo
            {
                ContainerNodeName = "MenuSounds",
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

            LoadSkins();
        }

        public void _CreateSkinButtonPressed()
        {
            string skinName = SkinNameEdit.Text;

            if (skinName.Length == 0)
                Dialog.Alert("Set a name for the new skin first.");

            if (Directory.Exists(SkinsFolder + "/" + skinName))
                Dialog.Alert("A skin with that name already exists.");

            var newSkinDir = Directory.CreateDirectory(SkinsFolder + "/" + skinName);

            foreach (var option in Options)
            {
                var skindir = new DirectoryInfo(GetNode<OptionButton>(option.NodePath).Text);
                foreach (var file in skindir.EnumerateFiles())
                {
                    if (!option.IncludeFileNames.Contains(Path.GetFileNameWithoutExtension(file.Name)))
                        continue;

                    file.CopyTo(newSkinDir.FullName);
                }
            }

            Dialog.Alert("Created skin.\n\nYou might need to press Ctrl+Shift+Alt+S in-game for the skin to appear.");
        }

        private void LoadSkins()
        {
            if (!Directory.Exists(SkinsFolder))
            {
                Dialog.TextInput(
                    text: "Couldn't find your skins folder, please set it below.",
                    action: p =>
                    {
                        SkinsFolder = p;
                        LoadSkins();
                    },
                    defaultText: SkinsFolder);
                return;
            }

            var directory = new DirectoryInfo(SkinsFolder);
            Skins = directory.EnumerateDirectories().Select(d => d.Name).ToArray();

            foreach (var option in Options)
            {
                var node = GetNode<OptionButton>(option.NodePath);
                node.Clear();
                node.AddItem("<< use default skin >>");
                foreach (var skin in Skins)
                    node.AddItem(skin);
            }
        }

        private class OptionInfo
        {
            public string ContainerNodeName { get; set; }

            public string NodePath => $"OptionsContainer/{ContainerNodeName}/OptionButton";

            public string[] IncludeFileNames { get; set; }
        }
    }
}