using Godot;
using System;
using System.IO;
using System.Linq;

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
            if (SkinNameEdit.Text.Length == 0)
                Dialog.Alert("Set a name for the new skin first.");
        }

        private void LoadSkins()
        {
            if (!System.IO.Directory.Exists(SkinsFolder))
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
                var node = GetNode<OptionButton>($"OptionsContainer/{option.ContainerNodeName}/OptionButton");
                node.Clear();
                node.AddItem("<< use default skin >>");
                foreach (var skin in Skins)
                    node.AddItem(skin);
            }
        }

        private class OptionInfo
        {
            public string ContainerNodeName { get; set; }

            public string[] IncludeFileNames { get; set; }
        }
    }
}