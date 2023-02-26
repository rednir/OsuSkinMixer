using Godot;

namespace OsuSkinMixer.Components;

public partial class SettingsPopup : Popup
{
	private Button ChangeSkinsFolderButton;
	private SetupPopup SetupPopup;

	public override void _Ready()
	{
		base._Ready();

		ChangeSkinsFolderButton = GetNode<Button>("%ChangeSkinsFolderButton");
		SetupPopup = GetNode<SetupPopup>("%SetupPopup");

		ChangeSkinsFolderButton.Pressed += SetupPopup.In;
	}
}
