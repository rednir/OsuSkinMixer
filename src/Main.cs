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
        public const string ROOT_VBOX_PATH = "ScrollContainer/CenterContainer/VBoxContainer";

        private readonly SkinOption[] Options = SkinOption.Default;

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
            else if (@event.IsActionPressed("randomize_bottom_level_options"))
                RandomizeBottomLevelOptions();
            else if (@event.IsActionPressed("randomize_top_level_options"))
                RandomizeTopLevelOptions();
        }

        private void _CreateSkinButtonPressed() => CreateSkin();

        #region Actions

        public void ResetSelections()
        {
            SkinNameEdit.Clear();
            foreach (var option in Options.Flatten(o => (o as ParentSkinOption)?.Children))
                option.OptionButton.SelectAndEmit(0);

            Toast.New("Reset selections!");
        }

        public void RandomizeTopLevelOptions()
        {
            var random = new Random();
            foreach (var option in Options)
                option.OptionButton.SelectAndEmit(random.Next(0, option.OptionButton.GetItemCount() - 1));

            Toast.New("Randomized top-level options!");
        }

        public void RandomizeBottomLevelOptions()
        {
            var random = new Random();
            foreach (var option in Options.Flatten(o => (o as ParentSkinOption)?.Children).Where(o => !(o is ParentSkinOption)))
                option.OptionButton.SelectAndEmit(random.Next(0, option.OptionButton.GetItemCount() - 1));

            Toast.New("Randomized bottom-level options!");
        }

        public void UseExistingSkin()
        {
            // All the OptionButtons should have equal `Items` anyway, so just get the first.
            /*var optionButton = GetNodeOrNull<OptionButton>(Options[0].NodePath);
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
            });*/
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
                SkinOptions = Options,
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

            var rootVbox = GetNode<VBoxContainer>(ROOT_VBOX_PATH);
            string[] skins = GetSkinNames();

            foreach (var option in Options)
                addOptionButton(option, rootVbox, 0);

            return true;

            // TODO: maintain option buttons when refreshing, and selection.
            void addOptionButton(SkinOption option, VBoxContainer vbox, int layer)
            {
                // Create new nodes for this option if not already existing.
                var hbox = (HBoxContainer)GetNode("OptionTemplate").Duplicate();
                var arrowButton = hbox.GetChild<TextureButton>(0);
                var label = hbox.GetChild<Label>(1);
                option.OptionButton = hbox.GetChild<OptionButton>(2);

                hbox.Name = option.Name;
                label.Text = option.Name;
                label.Modulate = new Color(1, 1, 1, 1f - (layer / 4f));
                hbox.HintTooltip = option.ToString().Wrap(100);

                option.OptionButton.AddItem("<< use default skin >>", 0);
                foreach (var skin in skins)
                    option.OptionButton.AddItem(skin, skin.GetHashCode());

                // For the ability to drag on the popup to move it.
                option.OptionButton.GetPopup().Connect("gui_input", this, nameof(_PopupGuiInput), new Godot.Collections.Array(option.OptionButton.GetPopup()));

                var indent = new Panel()
                {
                    RectMinSize = new Vector2(layer * 30, 1),
                    Modulate = new Color(0, 0, 0, 0),
                };

                // Indent needs to be the first node in the HBoxContainer.
                hbox.AddChild(indent);
                hbox.MoveChild(indent, 0);

                vbox.AddChild(hbox);

                if (option is ParentSkinOption parentOption)
                {
                    var newVbox = createVbox(parentOption);
                    vbox.AddChildBelowNode(hbox, newVbox);

                    arrowButton.Connect("toggled", this, nameof(_ArrowButtonPressed), new Godot.Collections.Array(newVbox));
                    option.OptionButton.Connect("item_selected", this, nameof(_OptionButtonItemSelected), new Godot.Collections.Array(parentOption));

                    foreach (var child in parentOption.Children)
                        addOptionButton(child, newVbox, layer + 1);
                }
                else
                {
                    // No children, so hide arrow button.
                    arrowButton.Disabled = true;
                    arrowButton.Modulate = new Color(0, 0, 0, 0);
                }
            }

            VBoxContainer createVbox(ParentSkinOption parentOption)
            {
                var vbox = new VBoxContainer()
                {
                    Name = $"{parentOption.Name}children",
                    MarginLeft = 20,
                    Visible = false,
                };

                vbox.AddConstantOverride("separation", 10);
                return vbox;
            }
        }

        private void _OptionButtonItemSelected(int index, ParentSkinOption option)
        {
            foreach (var child in option.Children)
                child.OptionButton.SelectAndEmit(index);
        }

        private void _PopupGuiInput(InputEvent @event, PopupMenu popupMenu)
        {
            if (@event is InputEventMouseMotion dragEvent && Input.IsMouseButtonPressed(2))
                popupMenu.RectPosition += new Vector2(0, dragEvent.Relative.y);
        }

        private void _ArrowButtonPressed(bool pressed, VBoxContainer vbox)
        {
            vbox.Visible = pressed;
        }

        #endregion
    }
}