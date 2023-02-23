using Godot;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Components;
using System.Collections.Generic;
using Skin = OsuSkinMixer.Models.Osu.Skin;
using System.Threading;
using System.Threading.Tasks;

namespace OsuSkinMixer.StackScenes;

public partial class SkinMixer : StackScene
{
    public override string Title => "Skin Mixer";

    private CancellationTokenSource CancellationTokenSource;

    private PackedScene SkinInfoScene;

    private SkinCreatorPopup SkinCreatorPopup;
    private SkinOptionsSelector SkinOptionsSelector;
    private Button CreateSkinButton;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

        SkinCreatorPopup = GetNode<SkinCreatorPopup>("SkinCreatorPopup");
        SkinOptionsSelector = GetNode<SkinOptionsSelector>("SkinOptionsSelector");
        CreateSkinButton = GetNode<Button>("CreateSkinButton");

        CreateSkinButton.Pressed += CreateSkinButtonPressed;
    }

    public void CreateSkinButtonPressed()
    {
        SkinCreatorPopup.In();

        SkinCreator skinCreator = new()
        {
            Name = "test",
            SkinOptions = SkinOptionsSelector.SkinOptions,
        };

        CancellationTokenSource = new CancellationTokenSource();
		Task.Run(async () => await skinCreator.CreateAndImportAsync(CancellationTokenSource.Token))
            .ContinueWith(t => {
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
            });
    }
}
