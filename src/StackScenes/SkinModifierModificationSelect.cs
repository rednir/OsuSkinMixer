namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Utils;
using OsuSkinMixer.Statics;

public partial class SkinModifierModificationSelect : StackScene
{
    public override string Title => SkinsToModify.Count == 1 ? $"Modifying: {SkinsToModify[0].Name}" : $"Modifying {SkinsToModify.Count} skins";

    private PackedScene SkinInfoScene;

    private CancellationTokenSource CancellationTokenSource;

    public List<OsuSkin> SkinsToModify { get; set; }

    private SkinOptionsSelector SkinOptionsSelector;
    private SkinComponent DefaultSkinComponent;
    private SkinComponent BlankComponent;
    private Button ApplyChangesButton;
    private ComboColoursContainer ComboColoursContainer;
    private CheckBox SmoothTrailCheckBox;
    private CheckBox InstafadeCheckBox;
    private CheckBox DisableAnimationsCheckBox;
    private LoadingPopup LoadingPopup;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

        SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
        DefaultSkinComponent = GetNode<SkinComponent>("%DefaultSkinComponent");
        BlankComponent = GetNode<SkinComponent>("%BlankComponent");
        ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
        ComboColoursContainer = GetNode<ComboColoursContainer>("%ComboColoursContainer");
        SmoothTrailCheckBox = GetNode<CheckBox>("%SmoothTrailCheckBox");
        InstafadeCheckBox = GetNode<CheckBox>("%InstafadeCheckBox");
        DisableAnimationsCheckBox = GetNode<CheckBox>("%DisableAnimationsCheckBox");
        LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");

        SkinOptionsSelector.CreateOptionComponents(new SkinOptionValue(SkinOptionValueType.Unchanged));
        DefaultSkinComponent.LeftClicked += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.DefaultSkin));
        BlankComponent.LeftClicked += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.Blank));
        ApplyChangesButton.Pressed += OnApplyChangesButtonPressed;
        ComboColoursContainer.Skin = SkinsToModify[0];
        LoadingPopup.CancelAction = OnCancelButtonPressed;
        LoadingPopup.DisableCancelAt = SkinModifierMachine.UNCANCELLABLE_AFTER;

        OsuData.SkinRemoved += OnSkinRemoved;
    }

    public override void _ExitTree()
    {
        OsuData.SkinRemoved -= OnSkinRemoved;
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
            SkinsToModify = SkinsToModify,
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
