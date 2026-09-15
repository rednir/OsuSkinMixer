namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Utils;
using OsuSkinMixer.Statics;
using System.IO;

public partial class SkinMixer : StackScene
{
    public override string Title => "Skin mixer";

    private CancellationTokenSource CancellationTokenSource;

    private PackedScene SkinInfoScene;

    private LoadingPopup LoadingPopup;
    private SkinNamePopup SkinNamePopup;
    private SkinOptionsSelector SkinOptionsSelector;
    private Button CreateSkinButton;
    private Button RandomButton;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

        LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");
        SkinNamePopup = GetNode<SkinNamePopup>("%SkinNamePopup");
        SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
        CreateSkinButton = GetNode<Button>("%CreateSkinButton");
        RandomButton = GetNode<Button>("%RandomButton");

        LoadingPopup.CancelAction = OnCancelButtonPressed;
        CreateSkinButton.Pressed += OnCreateSkinButtonPressed;
        RandomButton.Pressed += OnRandomButtonPressed;

        SkinNamePopup.ConfirmAction = RunSkinCreator;

        SkinOptionsSelector.CreateOptionComponents(SkinOptionValueType.DefaultSkin);
    }

    private void OnCreateSkinButtonPressed()
    {
        SkinNamePopup.In();
    }

    private void OnRandomButtonPressed()
    {
        SkinOptionsSelector.Randomize();

        EmitSignal(SignalName.ToastPushed, "Randomized skin options");
    }

    private async void RunSkinCreator(string skinName)
    {
        LoadingPopup.In();

        SkinMixerMachine machine = new()
        {
            SkinOptions = SkinOptionsSelector.SkinOptions,
            ProgressChanged = v => LoadingPopup.Progress = v,
            StatusChanged = v => LoadingPopup.Status = v,
        };

        machine.SetNewSkin(skinName);
        CancellationTokenSource = new CancellationTokenSource();

        Operation operation = null;
        operation = new Operation(
            type: OperationType.SkinMixer,
            targetSkin: machine.NewSkin,
            action: () =>
            {
                machine.Run(CancellationTokenSource.Token);
                operation.SetTarget(machine.NewSkin);
                operation.SetUndo(machine.UndoInstallation);

            },
            undoAction: () =>
            {
                machine.UndoInstallation?.Invoke();
            }
        );
        try
        {
            await operation.RunOperation(requestRefresh: false);
            if (!machine.Installed) return;
            // Scene creation and navigation must happen on Godot's main thread.
            var skinInfoInstance = SkinInfoScene.Instantiate<SkinInfo>();
            skinInfoInstance.Skins = new OsuSkin[] { machine.NewSkin };
            EmitSignal(SignalName.ScenePushed, skinInfoInstance);
        }
        catch (OperationCanceledException) { Settings.PushToast("Skin creation cancelled."); }
        catch (Exception e) { Settings.PushException(e); }
        finally
        {
            SkinNamePopup.Out();
            LoadingPopup.Out();
        }
    }

    private void OnCancelButtonPressed()
    {
        CancellationTokenSource?.Cancel();
    }
}
