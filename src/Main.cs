using Godot;
using System;
using System.IO;
using System.Linq;
using Directory = System.IO.Directory;
using Environment = System.Environment;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace OsuSkinMixer
{
    public class Main : Control
    {
        private CancellationTokenSource CancellationTokenSource { get; set; }

        private SkinOption[] SkinOptions { get; } = SkinOption.Default;

        private Skin[] Skins { get; set; }

        private SkinCreator SkinCreator { get; set; }

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
            OS.MinWindowSize = new Vector2(700, 450);
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

            if (TrySetSkins())
                CreateOptionButtons();
            else
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

        public override void _Process(float delta)
        {
            if (ProgressBar.Visible)
            {
                ProgressBar.Value = SkinCreator?.Progress ?? 0;
                ProgressBarLabel.Text = SkinCreator?.Status;
            }
        }

        private void _CreateSkinButtonPressed()
        {
            if (SkinCreator?.IsInProgress ?? false)
            {
                CreateSkinButton.Disabled = true;
                CancellationTokenSource.Cancel();
                return;
            }

            CreateSkin();
        }

        #region Actions

        public void ResetSelections()
        {
            SkinNameEdit.Clear();
            foreach (var option in SkinOption.Flatten(SkinOptions))
                option.OptionButton.SelectAndEmit(0);

            Toast.New("Reset selections!");
        }

        public void RandomizeTopLevelOptions()
        {
            var random = new Random();
            foreach (var option in SkinOptions)
                option.OptionButton.SelectAndEmit(random.Next(0, option.OptionButton.GetItemCount() - 1));

            Toast.New("Randomized top-level options!");
        }

        public void RandomizeBottomLevelOptions()
        {
            var random = new Random();
            foreach (var option in SkinOption.Flatten(SkinOptions).Where(o => !(o is ParentSkinOption)))
                option.OptionButton.SelectAndEmit(random.Next(0, option.OptionButton.GetItemCount() - 1));

            Toast.New("Randomized bottom-level options!");
        }

        public void UseExistingSkin()
        {
            // All the OptionButtons should have equal `Items` anyway, so just get the first.
            var optionButton = SkinOptions[0].OptionButton;

            Dialog.Options("Select a skin to use.", optionButton.Items, i =>
            {
                // This assumes that index 0 is default skin.
                SkinNameEdit.Text = i == 0 ? string.Empty : optionButton.GetItemText(i);

                foreach (var option in SkinOptions)
                    option.OptionButton.SelectAndEmit(i);
            });
        }

        public void RefreshSkins()
        {
            TrySetSkins();

            foreach (var option in SkinOption.Flatten(SkinOptions))
            {
                int selectedId = option.OptionButton.GetSelectedId();
                option.OptionButton.Clear();

                PopulateOptionButton(option.OptionButton);
                option.OptionButton.Select(option.OptionButton.GetItemIndex(selectedId));
            }

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

                    if (TrySetSkins())
                    {
                        CreateOptionButtons();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                },
                defaultText: Settings.Content.SkinsFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu!/Skins");
        }

        public void CreateSkin()
        {
            if (SkinCreator?.IsInProgress ?? false)
            {
                Toast.New("Skin creation is already in progress.");
                return;
            }

            SkinCreator = new SkinCreator()
            {
                Name = SkinNameEdit.Text.Replace(']', '-').Replace('[', '-'),
                SkinOptions = SkinOptions,
                Skins = Skins,
            };

            create(false);

            void create(bool overwrite)
            {
                setValues();
                CancellationTokenSource = new CancellationTokenSource();

                Task.Run(() => SkinCreator.Create(overwrite, CancellationTokenSource.Token))
                .ContinueWith(async t =>
                {
                    var ex = t.Exception?.InnerException;
                    if (ex is SkinExistsException)
                    {
                        Dialog.Question(
                            text: $"A skin with that name already exists. Replace it?\n\nThis will permanently remove '{SkinCreator.Name}'",
                            action: b =>
                            {
                                if (b)
                                    create(true);
                                else
                                    resetValues();
                            });

                        // Don't call resetValues immediately, wait until dialog is closed.
                        return;
                    }
                    else if (ex is SkinCreationInvalidException || ex is OperationCanceledException)
                    {
                        Toast.New(ex.Message);
                    }
                    else if (ex is Exception)
                    {
                        Logger.LogException(ex);
                        Dialog.Alert($"{ex.Message}\n\nPlease report this issue and attach logs.");
                    }
                    else
                    {
                        await finalize();
                    }

                    resetValues();
                });
            }

            void setValues()
            {
                SetOptionButtonsDisabled(true);
                CreateSkinButton.Disabled = false;
                CreateSkinButton.Text = "Cancel";
                ProgressBar.Visible = true;
            }

            void resetValues()
            {
                SetOptionButtonsDisabled(false);
                CreateSkinButton.Disabled = false;
                CreateSkinButton.Text = "Create skin";
                ProgressBar.Visible = false;
            }

            async Task finalize()
            {
                try
                {
                    if (Settings.Content.ImportToGameIfOpen && IsOsuOpen())
                    {
                        SkinCreator.TriggerOskImport();
                        Toast.New("Attempted to import skin into osu!");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    Toast.New("Couldn't import skin as an .osk file...");
                    await Task.Delay(800);
                }

                Toast.New("Created skin!\nYou might need to press Ctrl+Shift+Alt+S in-game.");
            }
        }

        private void SetOptionButtonsDisabled(bool value)
        {
            foreach (var option in SkinOption.Flatten(SkinOptions))
                option.OptionButton.Disabled = value;
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

        private bool TrySetSkins()
        {
            if (Settings.Content.SkinsFolder == null || !Directory.Exists(Settings.Content.SkinsFolder))
                return false;

            var result = new List<Skin>();
            var skinsFolder = new DirectoryInfo(Settings.Content.SkinsFolder);

            foreach (var dir in skinsFolder.EnumerateDirectories())
            {
                if (dir.Name != SkinCreator.WORKING_DIR_NAME)
                    result.Add(new Skin(dir));
            }

            Skins = result.OrderBy(s => s.Name).ToArray();
            return true;
        }

        #endregion

        #region Option buttons

        private void CreateOptionButtons()
        {
            var rootVbox = GetNode<VBoxContainer>("ScrollContainer/CenterContainer/VBoxContainer");

            foreach (var child in rootVbox.GetChildren())
                ((Node)child).QueueFree();

            foreach (var option in SkinOptions)
                addOptionButton(option, rootVbox, 0);

            void addOptionButton(SkinOption option, VBoxContainer vbox, int layer)
            {
                // Create new nodes for this option if not already existing.
                var hbox = (HBoxContainer)GetNode("OptionTemplate").Duplicate();
                var arrowButton = hbox.GetChild<TextureButton>(0);
                var label = hbox.GetChild<Label>(1);
                option.OptionButton = hbox.GetChild<OptionButton>(2);

                hbox.Name = option.Name;
                label.Text = option.Name;
                label.Modulate = new Color(1, 1, 1, Math.Max(1f - (layer / 4f), 0.55f));
                hbox.HintTooltip = option.ToString().Wrap(100);

                PopulateOptionButton(option.OptionButton);

                // For the ability to drag on the popup to move it.
                option.OptionButton.GetPopup().Connect("gui_input", this, nameof(_PopupGuiInput), new Godot.Collections.Array(option.OptionButton.GetPopup()));

                option.OptionButton.Connect("item_selected", this, nameof(_OptionButtonItemSelected), new Godot.Collections.Array(option));

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
                    var newVbox = CreateOptionChildrenVBox();
                    vbox.AddChildBelowNode(hbox, newVbox);

                    arrowButton.Connect("toggled", this, nameof(_ArrowButtonPressed), new Godot.Collections.Array(newVbox));

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
        }

        private VBoxContainer CreateOptionChildrenVBox()
        {
            var vbox = new VBoxContainer()
            {
                MarginLeft = 20,
                Visible = false,
            };

            vbox.AddConstantOverride("separation", 10);
            return vbox;
        }

        private void PopulateOptionButton(OptionButton optionButton)
        {
            optionButton.AddItem("<< use default skin >>", 0);
            foreach (var skin in Skins)
                optionButton.AddItem(skin.Name, skin.Name.GetHashCode());
        }

        private void _OptionButtonItemSelected(int index, SkinOption option)
        {
            string selectedSkin = option.OptionButton.Text;

            foreach (var parent in SkinOption.GetParents(option, SkinOptions))
                parent.OptionButton.Text = parent.Children.All(o => o.OptionButton.Text == selectedSkin) ? selectedSkin : "<< VARIOUS >>";

            if (option is ParentSkinOption parentOption)
            {
                foreach (var child in parentOption.Children)
                    child.OptionButton.SelectAndEmit(index);
            }
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