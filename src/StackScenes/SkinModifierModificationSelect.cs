using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Utils;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.StackScenes;

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
    private CheckButton SmoothTrailButton;
    private CheckButton InstafadeButton;
    private CheckButton DisableAnimationsButton;
    private LoadingPopup LoadingPopup;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

        SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
        DefaultSkinComponent = GetNode<SkinComponent>("%DefaultSkinComponent");
        BlankComponent = GetNode<SkinComponent>("%BlankComponent");
        ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
        SmoothTrailButton = GetNode<CheckButton>("%SmoothTrailButton");
        InstafadeButton = GetNode<CheckButton>("%InstafadeButton");
        DisableAnimationsButton = GetNode<CheckButton>("%DisableAnimationsButton");
        LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");

        SkinOptionsSelector.CreateOptionComponents(new SkinOptionValue(SkinOptionValueType.Unchanged));
        DefaultSkinComponent.LeftClicked += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.DefaultSkin));
        BlankComponent.LeftClicked += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.Blank));
        ApplyChangesButton.Pressed += OnApplyChangesButtonPressed;
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
            SmoothTrail = SmoothTrailButton.ButtonPressed,
            Instafade = InstafadeButton.ButtonPressed,
            DisableInterfaceAnimations = DisableAnimationsButton.ButtonPressed,
        };

        Task.Run(() => machine.Run(CancellationTokenSource.Token))
            .ContinueWith(t =>
            {
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
                InstafadeButton.ButtonPressed = false;
                SmoothTrailButton.ButtonPressed = false;
                DisableAnimationsButton.ButtonPressed = false;

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
