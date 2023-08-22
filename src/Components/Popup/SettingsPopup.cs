namespace OsuSkinMixer.Components;

using System.IO;
using System.Diagnostics;
using OsuSkinMixer.Statics;

public partial class SettingsPopup : Popup
{
    private Button UpdateButton;
    private CheckButton AutoUpdateButton;
    private CheckButton UseCompactSkinSelectorButton;
    private HSlider VolumeSlider;
    private AudioStreamPlayer VolumeTickSoundPlayer;
    private Button ChangeSkinsFolderButton;
    private Button ReportIssueButton;
    private Button OpenLogsButton;
    private SetupPopup SetupPopup;
    private LoadingPopup UpdateLoadingPopup;

    public override void _Ready()
    {
        base._Ready();

        UpdateButton = GetNode<Button>("%UpdateButton");
        AutoUpdateButton = GetNode<CheckButton>("%AutoUpdateButton");
        UseCompactSkinSelectorButton = GetNode<CheckButton>("%UseCompactSkinSelectorButton");
        VolumeSlider = GetNode<HSlider>("%VolumeSlider");
        VolumeTickSoundPlayer = GetNode<AudioStreamPlayer>("%VolumeTickSoundPlayer");
        ChangeSkinsFolderButton = GetNode<Button>("%ChangeSkinsFolderButton");
        ReportIssueButton = GetNode<Button>("%ReportIssueButton");
        OpenLogsButton = GetNode<Button>("%OpenLogsButton");
        SetupPopup = GetNode<SetupPopup>("%SetupPopup");
        UpdateLoadingPopup = GetNode<LoadingPopup>("%UpdateLoadingPopup");

        AutoUpdateButton.Disabled = OS.GetName() != "Windows";
        AutoUpdateButton.ButtonPressed = Settings.Content.AutoUpdate;
        UseCompactSkinSelectorButton.ButtonPressed = Settings.Content.UseCompactSkinSelector;
        VolumeSlider.Value = Settings.Content.Volume;

        UpdateButton.Pressed += UpdateButtonPressed;
        AutoUpdateButton.Pressed += AutoUpdateButtonPressed;
        VolumeSlider.ValueChanged += VolumeSliderValueChanged;
        VolumeSlider.DragEnded += VolumeSliderDragEnded;
        UseCompactSkinSelectorButton.Pressed += UseCompactSkinSelectorButtonPressed;
        ChangeSkinsFolderButton.Pressed += SetupPopup.In;
        ReportIssueButton.Pressed += () => OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/blob/master/FEEDBACK.md");
        OpenLogsButton.Pressed += () => Tools.ShellOpenFile(ProjectSettings.GlobalizePath("user://logs"));
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

    private void AutoUpdateButtonPressed()
    {
        Settings.Content.AutoUpdate = AutoUpdateButton.ButtonPressed;
        Settings.Save();
    }

    private void VolumeSliderValueChanged(double value)
    {
        Settings.Content.Volume = value;
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), (float)value);
        VolumeTickSoundPlayer.Play();
    }

    private void VolumeSliderDragEnded(bool valueChanged)
    {
        if (!valueChanged)
            return;

        Settings.Save();
    }

    private void UpdateButtonPressed()
    {
        if (!File.Exists(Settings.AutoUpdateInstallerPath))
        {
            OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/releases/latest");
            return;
        }

        Process.Start(Settings.AutoUpdateInstallerPath, "/silent");
    }
}
