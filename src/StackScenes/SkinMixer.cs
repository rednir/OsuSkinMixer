using Godot;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Components;
using System.Linq;
using System.Collections.Generic;
using Skin = OsuSkinMixer.Models.Osu.Skin;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace OsuSkinMixer.StackScenes;

public partial class SkinMixer : StackScene
{
    public override string Title => "Skin Mixer";

    private CancellationTokenSource CancellationTokenSource;

    private PackedScene SkinInfoScene;

    private SkinCreatorPopup SkinCreatorPopup;
    private SkinNamePopup SkinNamePopup;
    private SkinOptionsSelector SkinOptionsSelector;
    private Button CreateSkinButton;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

        SkinCreatorPopup = GetNode<SkinCreatorPopup>("SkinCreatorPopup");
        SkinNamePopup = GetNode<SkinNamePopup>("SkinNamePopup");
        SkinOptionsSelector = GetNode<SkinOptionsSelector>("SkinOptionsSelector");
        CreateSkinButton = GetNode<Button>("CreateSkinButton");

        CreateSkinButton.Pressed += CreateSkinButtonPressed;

        SkinNamePopup.ConfirmAction = s =>
        {
            SkinNamePopup.Out();
            RunSkinCreator(s);
        };

        SkinOptionsSelector.CreateOptionComponents("<<DEFAULT SKIN>>");
    }

    public void CreateSkinButtonPressed()
    {
        SkinNamePopup.In();
    }

    public void RunSkinCreator(string skinName)
    {
        SkinCreatorPopup.In();

        SkinCreator skinCreator = new()
        {
            Name = skinName,
            SkinOptions = SkinOptionsSelector.SkinOptions,
            ProgressChangedAction = (p, _) => SkinCreatorPopup.SetProgress(p),
        };

        CancellationTokenSource = new CancellationTokenSource();
        Task.Run(async () => await skinCreator.CreateAndImportAsync(CancellationTokenSource.Token))
            .ContinueWith(t =>
            {
                var ex = t.Exception;
                if (ex != null)
                {
                    GD.PrintErr(ex);
                    OS.Alert($"{ex.Message}\nPlease report this error with logs.", "Skin creation failure");
                    return;
                }

                var skinInfoInstance = SkinInfoScene.Instantiate<SkinInfo>();
                skinInfoInstance.SetSkin(t.Result);
                EmitSignal(SignalName.ScenePushed, skinInfoInstance);

                SkinCreatorPopup.Out();
            });
    }
}
