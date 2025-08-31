namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Utils;
using OsuSkinMixer.Statics;

public partial class SkinModifierModificationSelect : StackScene
{
    public override string Title => SkinsToModify.Count == 1 ? $"Modifying: {SkinsToModify[0].Name}" : $"Modifying {SkinsToModify.Count} skins";


    private CancellationTokenSource CancellationTokenSource;

    public List<OsuSkin> SkinsToModify { get; set; }

    private ComboColoursContainer[] ComboColoursContainers = [];
    private CursorColourContainer[] CursorColourContainers = [];

    private SkinOptionsSelector SkinOptionsSelector;
    private ExpandablePanelContainer ComboColourContainer;
    private ExpandablePanelContainer CursorColourContainer;
    private ExpandablePanelContainer ExtraOptionsContainer;
    private SkinComponent DefaultSkinComponent;
    private SkinComponent BlankComponent;
    private Button ApplyChangesButton;
    private VBoxContainer ComboColoursContainerCollection;
    private VBoxContainer CursorColourContainerCollection;
    private CheckBox SmoothTrailCheckBox;
    private CheckBox InstafadeCheckBox;
    private CheckBox DisableAnimationsCheckBox;
    private VBoxContainer WarningLabelContainer;
    private Label WarningLabelInstafadeColours;
    private LoadingPopup LoadingPopup;

    public override void _Ready()
    {
        SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
        ComboColourContainer = GetNode<ExpandablePanelContainer>("%ComboColourContainer");
        CursorColourContainer = GetNode<ExpandablePanelContainer>("%CursorColourContainer");
        ExtraOptionsContainer = GetNode<ExpandablePanelContainer>("%ExtraOptionsContainer");
        DefaultSkinComponent = GetNode<SkinComponent>("%DefaultSkinComponent");
        BlankComponent = GetNode<SkinComponent>("%BlankComponent");
        ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
        ComboColoursContainerCollection = GetNode<VBoxContainer>("%ComboColoursContainerCollection");
        CursorColourContainerCollection = GetNode<VBoxContainer>("%CursorColourContainerCollection");
        SmoothTrailCheckBox = GetNode<CheckBox>("%SmoothTrailCheckBox");
        InstafadeCheckBox = GetNode<CheckBox>("%InstafadeCheckBox");
        DisableAnimationsCheckBox = GetNode<CheckBox>("%DisableAnimationsCheckBox");
        WarningLabelContainer = GetNode<VBoxContainer>("%WarningLabelContainer");
        WarningLabelInstafadeColours = GetNode<Label>("%WarningLabelInstafadeColours");
        LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");

        SkinOptionsSelector.CreateOptionComponents(SkinOptionValueType.Unchanged);
        ComboColourContainer.ExpandChanged += _ => InitialiseComboColourContainers();
        CursorColourContainer.ExpandChanged += _ => InitialiseCursorColourContainers();
        DefaultSkinComponent.LeftClicked += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.DefaultSkin));
        BlankComponent.LeftClicked += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.Blank));
        ApplyChangesButton.Pressed += OnApplyChangesButtonPressed;
        LoadingPopup.CancelAction = OnCancelButtonPressed;
        LoadingPopup.DisableCancelAt = SkinModifierMachine.UNCANCELLABLE_AFTER;
        SmoothTrailCheckBox.Pressed += OnExperimentalOptionsStateChanged;
        InstafadeCheckBox.Pressed += OnExperimentalOptionsStateChanged;
        DisableAnimationsCheckBox.Pressed += OnExperimentalOptionsStateChanged;

        OsuData.SkinRemoved += OnSkinRemoved;
    }

    public override void _ExitTree()
    {
        OsuData.SkinRemoved -= OnSkinRemoved;
    }

    private void InitialiseComboColourContainers()
    {
        if (ComboColoursContainers.Length > 0)
            return;

        List<ComboColoursContainer> comboColourContainers = [];

        foreach (var skin in SkinsToModify)
        {
            var comboColoursContainer = GD.Load<PackedScene>("res://src/Components/Osu/ComboColoursContainer.tscn").Instantiate<ComboColoursContainer>();
            comboColoursContainer.Skin = skin;
            ComboColoursContainerCollection.AddChild(comboColoursContainer);
            comboColourContainers.Add(comboColoursContainer);
            comboColoursContainer.OverrideStateChanged += OnComboColourOverrideStateChanged;
        }

        ComboColoursContainers = [.. comboColourContainers];
    }

    private void InitialiseCursorColourContainers()
    {
        if (CursorColourContainers.Length > 0)
            return;

        List<CursorColourContainer> cursorColourContainers = [];

        foreach (var skin in SkinsToModify)
        {
            var cursorColourContainer = GD.Load<PackedScene>("res://src/Components/Osu/CursorColourContainer.tscn").Instantiate<CursorColourContainer>();
            cursorColourContainer.Skin = skin;
            CursorColourContainerCollection.AddChild(cursorColourContainer);
            cursorColourContainers.Add(cursorColourContainer);
            cursorColourContainer.OverrideStateChanged += OnCursorColourOverrideStateChanged;
        }

        CursorColourContainers = [.. cursorColourContainers];
    }

    private void OnComboColourOverrideStateChanged()
    {
        if (ComboColoursContainers.Any(c => c.OverrideEnabled))
        {
            ComboColourContainer.Activate();
        }
        else
        {
            ComboColourContainer.Deactivate();
        }
    }
    
    private void OnCursorColourOverrideStateChanged()
    {
        if (CursorColourContainers.Any(c => c.OverrideEnabled))
        {
            CursorColourContainer.Activate();
        }
        else
        {
            CursorColourContainer.Deactivate();
        }
    }

    private void OnExperimentalOptionsStateChanged()
    {
        if (SmoothTrailCheckBox.ButtonPressed || InstafadeCheckBox.ButtonPressed || DisableAnimationsCheckBox.ButtonPressed)
        {
            ExtraOptionsContainer.Activate();
        }
        else
        {
            ExtraOptionsContainer.Deactivate();
        }

        // TODO: when more warning labels are added treat these seperately.
        WarningLabelContainer.Visible = InstafadeCheckBox.ButtonPressed;
        WarningLabelInstafadeColours.Visible = InstafadeCheckBox.ButtonPressed;
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

        var comboColourOverrides = ComboColoursContainers.Where(c => c.OverrideEnabled)
            .ToDictionary(c => c.Skin.Name, c => c.ComboColourIcons.Select(i => i.Color).ToArray());

        var cursorColourOverrides = CursorColourContainers.Where(c => c.IsColourChosen)
            .ToDictionary(c => c.Skin.Name, c => c.GeneratedImagesDirPath);

        CancellationTokenSource = new CancellationTokenSource();
        SkinModifierMachine machine = new()
        {
            SkinOptions = SkinOptionsSelector.SkinOptions,
            ProgressChanged = v => LoadingPopup.Progress = v,
            StatusChanged = s => LoadingPopup.Status = s,
            SkinsToModify = SkinsToModify.ToArray(),
            SkinComboColourOverrides = comboColourOverrides,
            SkinCursorColourOverrideImageDirs = cursorColourOverrides,
            SmoothTrail = SmoothTrailCheckBox.ButtonPressed,
            Instafade = InstafadeCheckBox.ButtonPressed,
            DisableInterfaceAnimations = DisableAnimationsCheckBox.ButtonPressed,
        };

        Task.Run(() => machine.Run(CancellationTokenSource.Token))
            .ContinueWith(t =>
            {
                GodotThread.SetThreadSafetyChecksEnabled(false);

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

                var skinInfoInstance = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn").Instantiate<SkinInfo>();
                skinInfoInstance.Skins = SkinsToModify;
                EmitSignal(SignalName.ScenePushed, skinInfoInstance);
                
                LoadingPopup.Out();
            });
    }

    private void OnCancelButtonPressed()
    {
        CancellationTokenSource?.Cancel();
    }
}
