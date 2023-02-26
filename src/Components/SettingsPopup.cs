using Godot;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SettingsPopup : Popup
{
	private Button UpdateButton;
	private Button ChangeSkinsFolderButton;
	private Button ReportIssueButton;
	private Button OpenLogsButton;
	private SetupPopup SetupPopup;

	public override void _Ready()
	{
		base._Ready();

		UpdateButton = GetNode<Button>("%UpdateButton");
		ChangeSkinsFolderButton = GetNode<Button>("%ChangeSkinsFolderButton");
		ReportIssueButton = GetNode<Button>("%ReportIssueButton");
		OpenLogsButton = GetNode<Button>("%OpenLogsButton");
		SetupPopup = GetNode<SetupPopup>("%SetupPopup");

		UpdateButton.Pressed += () => OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/releases/latest");
		ChangeSkinsFolderButton.Pressed += SetupPopup.In;
		ReportIssueButton.Pressed += () => OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/issues/new/choose");
		OpenLogsButton.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath("user://logs"));
	}

	public void ShowUpdateButton()
	{
		UpdateButton.Visible = true;
	}
}
