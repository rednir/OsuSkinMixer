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
	private CheckButton UseCompactSkinSelectorButton;
	private Button ChangeSkinsFolderButton;
	private Button ReportIssueButton;
	private Button OpenLogsButton;
	private SetupPopup SetupPopup;
	private LoadingPopup UpdateLoadingPopup;

	public override void _Ready()
	{
		base._Ready();

		UpdateButton = GetNode<Button>("%UpdateButton");
		UseCompactSkinSelectorButton = GetNode<CheckButton>("%UseCompactSkinSelectorButton");
		ChangeSkinsFolderButton = GetNode<Button>("%ChangeSkinsFolderButton");
		ReportIssueButton = GetNode<Button>("%ReportIssueButton");
		OpenLogsButton = GetNode<Button>("%OpenLogsButton");
		SetupPopup = GetNode<SetupPopup>("%SetupPopup");
		UpdateLoadingPopup = GetNode<LoadingPopup>("%UpdateLoadingPopup");

		UpdateButton.Pressed += UpdateButtonPressed;
		UseCompactSkinSelectorButton.Pressed += UseCompactSkinSelectorButtonPressed;
		ChangeSkinsFolderButton.Pressed += SetupPopup.In;
		ReportIssueButton.Pressed += () => OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/issues/new/choose");
		OpenLogsButton.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath("user://logs"));

		UseCompactSkinSelectorButton.ButtonPressed = Settings.Content.UseCompactSkinSelector;
	}

	public void ShowUpdateButton()
	{
		UpdateButton.Visible = true;
	}

	private void UseCompactSkinSelectorButtonPressed()
	{
		Settings.Content.UseCompactSkinSelector = UseCompactSkinSelectorButton.ButtonPressed;
		Settings.Save();
	}

	private void UpdateButtonPressed()
	{
		if (OS.GetName() != "Windows")
		{
			OpenDownloadsPage();
			return;
		}

		UpdateLoadingPopup.In();
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

					UpdateLoadingPopup.SetProgress(100);
					Process.Start(installerPath, "/silent");
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
