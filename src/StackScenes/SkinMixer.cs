using Godot;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Components;
using System.Threading;
using System.Threading.Tasks;

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
	private Button RandomButton;

	public override void _Ready()
	{
		SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

		SkinCreatorPopup = GetNode<SkinCreatorPopup>("%SkinCreatorPopup");
		SkinNamePopup = GetNode<SkinNamePopup>("%SkinNamePopup");
		SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
		CreateSkinButton = GetNode<Button>("%CreateSkinButton");
		RandomButton = GetNode<Button>("%RandomButton");

		CreateSkinButton.Pressed += OnCreateSkinButtonPressed;
		RandomButton.Pressed += OnRandomButtonPressed;

		SkinNamePopup.ConfirmAction = s =>
		{
			SkinNamePopup.Out();
			RunSkinCreator(s);
		};

		SkinOptionsSelector.CreateOptionComponents("<<DEFAULT SKIN>>");
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
				skinInfoInstance.Skin = t.Result;
				EmitSignal(SignalName.ScenePushed, skinInfoInstance);

				SkinCreatorPopup.Out();
				EmitSignal(SignalName.ToastPushed, "Successfully created and imported skin.");
			});
	}
}
