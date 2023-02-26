using Godot;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SettingsPopup : Popup
{
	private Button UpdateButton;
	private Button ChangeSkinsFolderButton;
	private SetupPopup SetupPopup;

	public override void _Ready()
	{
		base._Ready();

		UpdateButton = GetNode<Button>("%UpdateButton");
		ChangeSkinsFolderButton = GetNode<Button>("%ChangeSkinsFolderButton");
		SetupPopup = GetNode<SetupPopup>("%SetupPopup");

		UpdateButton.Pressed += () => OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/releases/latest");
		ChangeSkinsFolderButton.Pressed += SetupPopup.In;
	}

	public void ShowUpdateButton()
	{
		UpdateButton.Visible = true;
	}
}
