namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Utils;
using OsuSkinMixer.Statics;

public partial class SkinModifierModificationSelect : StackScene
{
    public override string Title => SkinsToModify.Count == 1 ? $"Modifying: {SkinsToModify[0].Name}" : $"Modifying {SkinsToModify.Count} skins";

    private PackedScene SkinInfoScene;
    private PackedScene ComboColourContainerScene;
    private PackedScene CursorColourContainerScene;

    private CancellationTokenSource CancellationTokenSource;

    public List<OsuSkin> SkinsToModify { get; set; }

    private ComboColoursContainer[] ComboColoursContainers;
    private CursorColourContainer[] CursorColourContainers;

    private SkinOptionsSelector SkinOptionsSelector;
    private SkinComponent DefaultSkinComponent;
    private SkinComponent BlankComponent;
    private Button ApplyChangesButton;
    private VBoxContainer ComboColoursContainerCollection;
    private VBoxContainer CursorColourContainerCollection;
    private CheckBox SmoothTrailCheckBox;
    private CheckBox InstafadeCheckBox;
    private CheckBox DisableAnimationsCheckBox;
    private LoadingPopup LoadingPopup;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");
        ComboColourContainerScene = GD.Load<PackedScene>("res://src/Components/Osu/ComboColoursContainer.tscn");
        CursorColourContainerScene = GD.Load<PackedScene>("res://src/Components/Osu/CursorColourContainer.tscn");

        SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
        DefaultSkinComponent = GetNode<SkinComponent>("%DefaultSkinComponent");
        BlankComponent = GetNode<SkinComponent>("%BlankComponent");
        ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
        ComboColoursContainerCollection = GetNode<VBoxContainer>("%ComboColoursContainerCollection");
        CursorColourContainerCollection = GetNode<VBoxContainer>("%CursorColourContainerCollection");
        SmoothTrailCheckBox = GetNode<CheckBox>("%SmoothTrailCheckBox");
        InstafadeCheckBox = GetNode<CheckBox>("%InstafadeCheckBox");
        DisableAnimationsCheckBox = GetNode<CheckBox>("%DisableAnimationsCheckBox");
        LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");

        SkinOptionsSelector.CreateOptionComponents(new SkinOptionValue(SkinOptionValueType.Unchanged));
        DefaultSkinComponent.LeftClicked += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.DefaultSkin));
        BlankComponent.LeftClicked += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.Blank));
        ApplyChangesButton.Pressed += OnApplyChangesButtonPressed;
        LoadingPopup.CancelAction = OnCancelButtonPressed;
        LoadingPopup.DisableCancelAt = SkinModifierMachine.UNCANCELLABLE_AFTER;

        OsuData.SkinRemoved += OnSkinRemoved;

        InitialiseColourOverrideContainers();
    }

    public override void _ExitTree()
    {
        OsuData.SkinRemoved -= OnSkinRemoved;
    }

    private void InitialiseColourOverrideContainers()
    {
        List<ComboColoursContainer> comboColourContainers = new();
        List<CursorColourContainer> cursorColourContainers = new();

        foreach (var skin in SkinsToModify)
        {
            var comboColoursContainer = ComboColourContainerScene.Instantiate<ComboColoursContainer>();
            comboColoursContainer.Skin = skin;
            ComboColoursContainerCollection.AddChild(comboColoursContainer);
            comboColourContainers.Add(comboColoursContainer);

            var cursorColourContainer = CursorColourContainerScene.Instantiate<CursorColourContainer>();
            cursorColourContainer.Skin = skin;
            CursorColourContainerCollection.AddChild(cursorColourContainer);
            cursorColourContainers.Add(cursorColourContainer);
        }

        ComboColoursContainers = comboColourContainers.ToArray();
        CursorColourContainers = cursorColourContainers.ToArray();
    }

    private void OnSkinRemoved(OsuSkin skin)
    {
        if (SkinsToModify.Remove(skin) && SkinsToModify.Count == 0)
        {
            if (Visible)
            {
                EmitSignal(SignalName.ScenePopped);
                return;
            }

            ApplyChangesButton.Disabled = true;
        }
    }

    private void OnApplyChangesButtonPressed()
    {
        LoadingPopup.In();

        var overrides = ComboColoursContainers.Where(c => c.OverrideEnabled)
            .ToDictionary(c => c.Skin.Name, c => c.ComboColourIcons.Select(i => i.Color).ToArray());

        CancellationTokenSource = new CancellationTokenSource();
        SkinModifierMachine machine = new()
        {
            SkinOptions = SkinOptionsSelector.SkinOptions,
            ProgressChanged = v => LoadingPopup.Progress = v,
            StatusChanged = s => LoadingPopup.Status = s,
            SkinsToModify = SkinsToModify.ToArray(),
            SkinComboColourOverrides = overrides,
            SmoothTrail = SmoothTrailCheckBox.ButtonPressed,
            Instafade = InstafadeCheckBox.ButtonPressed,
            DisableInterfaceAnimations = DisableAnimationsCheckBox.ButtonPressed,
        };

        Task.Run(() => machine.Run(CancellationTokenSource.Token))
            .ContinueWith(t =>
            {
                GodotThread.SetThreadSafetyChecksEnabled(false);
                LoadingPopup.Out();

                var ex = t.Exception;
                if (ex != null)
                {
                    if (ex.InnerException is OperationCanceledException)
                        return;

                    Settings.PushException(ex);
                    return;
                }

                SkinOptionsSelector.Reset();
                InstafadeCheckBox.ButtonPressed = false;
                SmoothTrailCheckBox.ButtonPressed = false;
                DisableAnimationsCheckBox.ButtonPressed = false;
                
                foreach (var container in ComboColoursContainers)
                    container.CallDeferred(nameof(container.Reset));

                var skinInfoInstance = SkinInfoScene.Instantiate<SkinInfo>();
                skinInfoInstance.Skins = SkinsToModify;
                EmitSignal(SignalName.ScenePushed, skinInfoInstance);
            });
    }

    private void OnCancelButtonPressed()
    {
        CancellationTokenSource?.Cancel();
    }
}
