using System;
using System.IO;
using System.Diagnostics;
using Godot;
using HttpClient = System.Net.Http.HttpClient;
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

		UpdateButton.Pressed += () => UpdateButtonPressed();
		ChangeSkinsFolderButton.Pressed += SetupPopup.In;
		ReportIssueButton.Pressed += () => OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/issues/new/choose");
		OpenLogsButton.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath("user://logs"));
	}

	public void ShowUpdateButton()
	{
		UpdateButton.Visible = true;
	}

	private void UpdateButtonPressed()
	{
		if (OS.GetName() != "Windows")
		{
			OpenDownloadsPage();
			return;
		}

		UpdateButton.Disabled = true;
		UpdateButton.Text = "Downloading...";
		HttpClient client = new();

        client.GetStreamAsync($"https://github.com/{Settings.GITHUB_REPO_PATH}/releases/latest/download/osu-skin-mixer-setup.exe")
			.ContinueWith(async t =>
			{
				if (t.Exception != null)
				{
                    OpenDownloadsPage(t.Exception);
					return;
				}

				try
				{
					string installerPath = $"{Path.GetTempPath()}osu-skin-mixer-setup.exe";

					if (File.Exists(installerPath))
						File.Delete(installerPath);

					FileStream fileStream = new(installerPath, FileMode.CreateNew);
					await t.Result.CopyToAsync(fileStream);
					t.Result.Dispose();
					fileStream.Dispose();

					Process.Start(installerPath, "/silent");
					UpdateButton.Text = "Installing...";
				}
				catch (Exception ex)
				{
                    OpenDownloadsPage(ex);
				}
			});
    }

	private static void OpenDownloadsPage(Exception ex = null)
	{
		if (ex != null)
		{
			GD.PrintErr(ex.ToString());
			OS.Alert($"Failed to automatically install the update, please report this issue\n({ex.Message})\n\nYou should manually download the update from the releases page instead.");
		}

		OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/releases/latest");
	}
}
