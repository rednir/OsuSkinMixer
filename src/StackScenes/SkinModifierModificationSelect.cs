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

    private CancellationTokenSource CancellationTokenSource;

    public List<OsuSkin> SkinsToModify { get; set; }

    private ComboColoursContainer[] ComboColoursContainers;

    private SkinOptionsSelector SkinOptionsSelector;
    private SkinComponent DefaultSkinComponent;
    private SkinComponent BlankComponent;
    private Button ApplyChangesButton;
    private VBoxContainer ComboColoursContainerCollection;
    private CheckBox SmoothTrailCheckBox;
    private CheckBox InstafadeCheckBox;
    private CheckBox DisableAnimationsCheckBox;
    private LoadingPopup LoadingPopup;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");
        ComboColourContainerScene = GD.Load<PackedScene>("res://src/Components/Osu/ComboColoursContainer.tscn");

        SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
        DefaultSkinComponent = GetNode<SkinComponent>("%DefaultSkinComponent");
        BlankComponent = GetNode<SkinComponent>("%BlankComponent");
        ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
        ComboColoursContainerCollection = GetNode<VBoxContainer>("%ComboColoursContainerCollection");
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

        InitialiseComboColourContainers();
    }

    public override void _ExitTree()
    {
        OsuData.SkinRemoved -= OnSkinRemoved;
    }

    private void InitialiseComboColourContainers()
    {
        List<ComboColoursContainer> containers = new();

        foreach (var skin in SkinsToModify)
        {
            var container = ComboColourContainerScene.Instantiate<ComboColoursContainer>();
            container.Skin = skin;
            ComboColoursContainerCollection.AddChild(container);
            containers.Add(container);
        }

        ComboColoursContainers = containers.ToArray();
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

        CancellationTokenSource = new CancellationTokenSource();
        SkinModifierMachine machine = new()
        {
            SkinOptions = SkinOptionsSelector.SkinOptions,
            ProgressChanged = v => LoadingPopup.Progress = v,
            StatusChanged = s => LoadingPopup.Status = s,
            SkinsToModify = SkinsToModify.ToArray(),
            ComboColoursContainers = ComboColoursContainers,
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
