using Godot;
using System;
using System.IO;
using System.Linq;
using Directory = System.IO.Directory;
using Environment = System.Environment;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OsuSkinMixer
{
    public class Main : Control
    {
        public const string VBOX_CONTAINER_PATH = "ScrollContainer/CenterContainer/VBoxContainer";

        private readonly OptionInfo[] Options = SkinOptions.Default;

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
            foreach (var option in Options)
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

            foreach (var option in Options)
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
            var optionButton = GetNodeOrNull<OptionButton>(Options[0].NodePath);
            if (optionButton == null)
                return;

            Dialog.Options("Select a skin to use.", optionButton.Items, i =>
            {
                // This assumes that index 0 is default skin.
                SkinNameEdit.Text = i == 0 ? string.Empty : optionButton.GetItemText(i);

                foreach (var option in Options)
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
            var creator = new SkinCreator()
            {
                Name = SkinNameEdit.Text.Replace(']', '-').Replace('[', '-'),
                Options = Options,
                ProgressSetter = (v, t) =>
                {
                    ProgressBar.Value = v;
                    ProgressBarLabel.Text = t;
                },
            };

            create(false);

            void create(bool overwrite)
            {
                CreateSkinButton.Disabled = true;
                ProgressBar.Visible = true;

                Task.Run(async () =>
                {
                    creator.Create(overwrite);
                    await finalize();
                })
                .ContinueWith(t =>
                {
                    CreateSkinButton.Disabled = false;
                    ProgressBar.Visible = false;

                    var ex = t.Exception?.InnerException;
                    if (ex is SkinExistsException)
                    {
                        Dialog.Question(
                            text: $"A skin with that name already exists. Replace it?\n\nThis will permanently remove '{creator.Name}'",
                            action: b =>
                            {
                                if (b)
                                    create(true);
                            });
                    }
                    else if (ex is SkinCreationInvalidException)
                    {
                        Toast.New(ex.Message);
                    }
                    else if (ex is Exception)
                    {
                        Logger.Log($"Exception thrown:\n\n{ex}\n\n");
                        Dialog.Alert($"Something went wrong.\n\n{ex.Message}");
                    }
                });
            }

            async Task finalize()
            {
                try
                {
                    if (Settings.Content.ImportToGameIfOpen && IsOsuOpen())
                    {
                        creator.TriggerOskImport();
                        Toast.New("Attempted to import skin into osu!");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Couldn't import skin as an .osk file:\n\n{ex}\n\n");
                    Toast.New("Couldn't import skin as an .osk file...");
                    await Task.Delay(800);
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
            .Select(d => d.Name).Where(n => n != SkinCreator.WORKING_DIR_NAME).OrderBy(n => n).ToArray();

        #endregion

        #region Option buttons

        private bool CreateOptionButtons()
        {
            if (Settings.Content.SkinsFolder == null || !Directory.Exists(Settings.Content.SkinsFolder))
                return false;

            var vbox = GetNode(VBOX_CONTAINER_PATH);
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
                        suboption.OptionButton = optionButton;
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