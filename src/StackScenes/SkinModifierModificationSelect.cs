using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierModificationSelect : StackScene
{
	public override string Title => SkinsToModify.Count == 1 ? $"Modifying: {SkinsToModify[0].Name}" : $"Modifying {SkinsToModify.Count} skins";

	private PackedScene SkinInfoScene;

    private CancellationTokenSource CancellationTokenSource;

	public List<OsuSkin> SkinsToModify { get; set; }

	private SkinOptionsSelector SkinOptionsSelector;
	private Button ApplyChangesButton;
	private LoadingPopup LoadingPopup;

	public override void _Ready()
	{
		SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

		SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
		ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
		LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");

		SkinOptionsSelector.CreateOptionComponents("<<UNCHANGED>>");
		ApplyChangesButton.Pressed += OnApplyChangesButtonPressed;
		LoadingPopup.CancelAction = OnCancelButtonPressed;
	}

	private void OnApplyChangesButtonPressed()
	{
		LoadingPopup.In();

		CancellationTokenSource = new CancellationTokenSource();
		SkinModifier skinModifier = new()
		{
			SkinOptions = SkinOptionsSelector.SkinOptions,
			ProgressChangedAction = LoadingPopup.SetProgress,
			SkinsToModify = SkinsToModify,
		};

		Task.Run(() => skinModifier.ModifySkins(CancellationTokenSource.Token))
	        .ContinueWith(t =>
            {
                LoadingPopup.Out();

                var ex = t.Exception;
                if (ex != null)
                {
					if (ex.InnerException is OperationCanceledException)
						return;

                    GD.PrintErr(ex);
                    OS.Alert($"{ex.Message}\nPlease report this error with logs.", "Skin creation failure");
                    return;
                }

				SkinOptionsSelector.Reset();

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
