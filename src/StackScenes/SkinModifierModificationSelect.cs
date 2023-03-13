using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Utils;

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
	private Button ExtraOptionsButton;
	private OkPopup ExtraOptionsPopup;
	private CheckButton SmoothTrailButton;
	private CheckButton InstafadeButton;
	private LoadingPopup LoadingPopup;

	public override void _Ready()
	{
		SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

		SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
		DefaultSkinComponent = GetNode<SkinComponent>("%DefaultSkinComponent");
		BlankComponent = GetNode<SkinComponent>("%BlankComponent");
		ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
		ExtraOptionsButton = GetNode<Button>("%ExtraOptionsButton");
		ExtraOptionsPopup = GetNode<OkPopup>("%ExtraOptionsPopup");
		SmoothTrailButton = GetNode<CheckButton>("%SmoothTrailButton");
		InstafadeButton = GetNode<CheckButton>("%InstafadeButton");
		LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");

		SkinOptionsSelector.CreateOptionComponents(new SkinOptionValue(SkinOptionValueType.Unchanged));
		DefaultSkinComponent.Pressed += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.DefaultSkin));
		BlankComponent.Pressed += () => SkinOptionsSelector.OptionComponentSelected(new SkinOptionValue(SkinOptionValueType.Blank));
		ExtraOptionsButton.Pressed += ExtraOptionsPopup.In;
		ApplyChangesButton.Pressed += OnApplyChangesButtonPressed;
		LoadingPopup.CancelAction = OnCancelButtonPressed;
		LoadingPopup.DisableCancelAt = SkinModifierMachine.UNCANCELLABLE_AFTER;
	}

	private void OnApplyChangesButtonPressed()
	{
		LoadingPopup.In();

		CancellationTokenSource = new CancellationTokenSource();
		SkinModifierMachine machine = new()
		{
			SkinOptions = SkinOptionsSelector.SkinOptions,
			ProgressChanged = v => LoadingPopup.Progress = v,
			SkinsToModify = SkinsToModify,
			SmoothTrail = SmoothTrailButton.ButtonPressed,
			Instafade = InstafadeButton.ButtonPressed,
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

                    GD.PrintErr(ex);
                    OS.Alert($"{ex.Message}\nPlease report this error with logs.", "Skin creation failure");
                    return;
                }

				SkinOptionsSelector.Reset();
				InstafadeButton.ButtonPressed = false;
				SmoothTrailButton.ButtonPressed = false;

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
