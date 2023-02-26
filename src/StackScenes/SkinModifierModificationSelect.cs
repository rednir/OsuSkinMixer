using Godot;
using System;
using System.Collections.Generic;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Components;
using System.Threading;
using System.Threading.Tasks;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierModificationSelect : StackScene
{
	public override string Title => SkinsToModify.Count == 1 ? $"Modifying: {SkinsToModify[0].Name}" : $"Modifying {SkinsToModify.Count} skins";

    private CancellationTokenSource CancellationTokenSource;

	public List<OsuSkin> SkinsToModify { get; set; }

	private SkinOptionsSelector SkinOptionsSelector;
	private Button ApplyChangesButton;
	private SkinCreatorPopup SkinCreatorPopup;

	public override void _Ready()
	{
		SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
		ApplyChangesButton = GetNode<Button>("%ApplyChangesButton");
		SkinCreatorPopup = GetNode<SkinCreatorPopup>("%SkinCreatorPopup");

		SkinOptionsSelector.CreateOptionComponents("<<UNCHANGED>>");
		ApplyChangesButton.Pressed += OnApplyChangesButtonPressed;
	}

	private void OnApplyChangesButtonPressed()
	{
		SkinCreatorPopup.In();

		CancellationTokenSource = new CancellationTokenSource();
		SkinCreator skinCreator = new()
		{
			SkinOptions = SkinOptionsSelector.SkinOptions,
			ProgressChangedAction = (p, _) => SkinCreatorPopup.SetProgress(p),
		};

		Task.Run(() => skinCreator.ModifySkins(SkinsToModify, CancellationTokenSource.Token))
	        .ContinueWith(t =>
            {
                var ex = t.Exception;
                if (ex != null)
                {
                    GD.PrintErr(ex);
                    OS.Alert($"{ex.Message}\nPlease report this error with logs.", "Skin creation failure");
                    return;
                }

                SkinCreatorPopup.Out();
            });
	}
}
