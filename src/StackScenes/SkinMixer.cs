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

        SkinOptionsSelector.CreateOptionComponents(new SkinOptionValue(SkinOptionValueType.DefaultSkin));
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

    private void RunSkinCreator(string skinName)
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

        new Operation(
            type: OperationType.SkinMixer,
            targetSkin: machine.NewSkin,
            action: () =>
            {
                machine.Run(CancellationTokenSource.Token);

                var skinInfoInstance = SkinInfoScene.Instantiate<SkinInfo>();
                skinInfoInstance.Skins = new OsuSkin[] { machine.NewSkin };
                EmitSignal(SignalName.ScenePushed, skinInfoInstance);
            },
            undoAction: () =>
            {
                if (!Directory.Exists(machine.NewSkin.Directory.FullName))
                    return;

                machine.NewSkin.Directory.Delete(true);
                OsuData.RemoveSkin(machine.NewSkin);
            }
        )
        .RunOperation()
        .ContinueWith(_ =>
        {
            SkinNamePopup.Out();
            LoadingPopup.Out();
        });
    }

    private void OnCancelButtonPressed()
    {
        CancellationTokenSource?.Cancel();
    }
}
