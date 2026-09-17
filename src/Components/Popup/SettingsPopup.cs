namespace OsuSkinMixer.Components;

using System.IO;
using System.Diagnostics;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Storage;

public partial class SettingsPopup : Popup
{
    private Button UpdateButton;
    private CheckButton AutoUpdateButton;
    private CheckButton UseCompactSkinSelectorButton;
    private CheckButton NotifyOnSkinFolderChangeButton;
    private CheckButton DisableHiDpiScalingButton;
    private HSlider VolumeSlider;
    private AudioStreamPlayer VolumeTickSoundPlayer;
    private Button ChangeSkinsFolderButton;
    private Button ReportIssueButton;
    private Button OpenLogsButton;
    private VBoxContainer LazerBackupContainer;
    private Label LazerBackupStatus;
    private Button OpenLazerBackupsButton;
    private SetupPopup SetupPopup;
    private LoadingPopup UpdateLoadingPopup;
    private Label LibraryStatus;

    public override void _Ready()
    {
        base._Ready();

        ConfigFile engineOverrides = new();
        engineOverrides.Load(Settings.EngineOverridesFilePath);

        UpdateButton = GetNode<Button>("%UpdateButton");
        AutoUpdateButton = GetNode<CheckButton>("%AutoUpdateButton");
        UseCompactSkinSelectorButton = GetNode<CheckButton>("%UseCompactSkinSelectorButton");
        NotifyOnSkinFolderChangeButton = GetNode<CheckButton>("%NotifyOnSkinFolderChangeButton");
        DisableHiDpiScalingButton = GetNode<CheckButton>("%DisableHiDpiScalingButton");
        VolumeSlider = GetNode<HSlider>("%VolumeSlider");
        VolumeTickSoundPlayer = GetNode<AudioStreamPlayer>("%VolumeTickSoundPlayer");
        ChangeSkinsFolderButton = GetNode<Button>("%ChangeSkinsFolderButton");
        LibraryStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(400, 0) };
        ChangeSkinsFolderButton.GetParent().AddChild(LibraryStatus);
        ReportIssueButton = GetNode<Button>("%ReportIssueButton");
        OpenLogsButton = GetNode<Button>("%OpenLogsButton");
        LazerBackupContainer = GetNode<VBoxContainer>("%LazerBackupContainer");
        LazerBackupStatus = GetNode<Label>("%LazerBackupStatus");
        OpenLazerBackupsButton = GetNode<Button>("%OpenLazerBackupsButton");
        SetupPopup = GetNode<SetupPopup>("%SetupPopup");
        UpdateLoadingPopup = GetNode<LoadingPopup>("%UpdateLoadingPopup");

        AutoUpdateButton.Disabled = OS.GetName() != "Windows";
        AutoUpdateButton.ButtonPressed = Settings.Content.AutoUpdate;
        UseCompactSkinSelectorButton.ButtonPressed = Settings.Content.UseCompactSkinSelector;
        NotifyOnSkinFolderChangeButton.ButtonPressed = Settings.Content.NotifyOnSkinFolderChange;
        DisableHiDpiScalingButton.ButtonPressed = !(bool)engineOverrides.GetValue("display", "window/dpi/allow_hidpi", true);
        VolumeSlider.Value = Settings.Content.Volume;

        UpdateButton.Pressed += UpdateButtonPressed;
        AutoUpdateButton.Pressed += AutoUpdateButtonPressed;
        VolumeSlider.ValueChanged += VolumeSliderValueChanged;
        VolumeSlider.DragEnded += VolumeSliderDragEnded;
        UseCompactSkinSelectorButton.Pressed += UseCompactSkinSelectorButtonPressed;
        NotifyOnSkinFolderChangeButton.Pressed += NotifyOnSkinFolderChangeButtonPressed;
        DisableHiDpiScalingButton.Pressed += DisableHiDpiScalingButtonPressed;
        ChangeSkinsFolderButton.Pressed += SetupPopup.In;
        ReportIssueButton.Pressed += () => OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/blob/master/FEEDBACK.md");
        OpenLogsButton.Pressed += () => Tools.ShellOpenFile(ProjectSettings.GlobalizePath("user://"));
        OpenLazerBackupsButton.Pressed += OpenLazerBackupsButtonPressed;
    }

    public void ShowUpdateButton()
    {
        UpdateButton.SetDeferred("visible", true);
    }

    public override void In()
    {
        LibraryStatus.Text = $"{OsuData.Library?.Status}\n{OsuData.Library?.Root}";
        RefreshLazerBackups();
        base.In();
    }

    private void RefreshLazerBackups()
    {
        if (OsuData.Library is not LazerSkinLibrary library)
        {
            LazerBackupContainer.Visible = false;
            return;
        }

        LazerBackupContainer.Visible = true;
        OpenLazerBackupsButton.TooltipText = library.BackupDirectory;
        try
        {
            var backups = library.GetBackupFiles();
            if (backups.Count == 0)
            {
                LazerBackupStatus.Text = "No backups yet.";
                return;
            }

            DateTime latest = File.GetLastWriteTime(backups[0]);
            string noun = backups.Count == 1 ? "backup" : "backups";
            LazerBackupStatus.Text = $"{backups.Count} database {noun} available. Latest: {latest:g}.";
        }
        catch (Exception e)
        {
            Settings.Log($"Could not inspect lazer backups: {e.Message}");
            LazerBackupStatus.Text = "The backup folder could not be inspected. You can still try opening it below.";
        }
    }

    private void OpenLazerBackupsButtonPressed()
    {
        if (OsuData.Library is not LazerSkinLibrary library) return;
        try
        {
            library.EnsureBackupDirectory();
            Tools.ShellOpenFile(library.BackupDirectory);
        }
        catch (Exception e) { Settings.PushException(new IOException("Could not open the lazer backup folder.", e)); }
    }

    private void UseCompactSkinSelectorButtonPressed()
    {
        Settings.Content.UseCompactSkinSelector = UseCompactSkinSelectorButton.ButtonPressed;
        Settings.Save();
    }

    private void NotifyOnSkinFolderChangeButtonPressed()
    {
        Settings.Content.NotifyOnSkinFolderChange = NotifyOnSkinFolderChangeButton.ButtonPressed;
        Settings.Save();
    }

    private void DisableHiDpiScalingButtonPressed()
    {
        string path = Settings.EngineOverridesFilePath;
        ConfigFile config = new();

        config.Load(path);
        config.SetValue("display", "window/dpi/allow_hidpi", !DisableHiDpiScalingButton.ButtonPressed);
        Error err = config.Save(path);

        Settings.PushToast(err == Error.Ok ? "Changes will take effect after relaunching the app." : "Failed to change setting.");
        Out();
    }

    private void AutoUpdateButtonPressed()
    {
        Settings.Content.AutoUpdate = AutoUpdateButton.ButtonPressed;
        Settings.Save();
    }

    private void VolumeSliderValueChanged(double value)
    {
        Settings.Content.Volume = value;
        Settings.Content.VolumeMute = value <= VolumeSlider.MinValue;
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), (float)value);
        AudioServer.SetBusMute(AudioServer.GetBusIndex("Master"), Settings.Content.VolumeMute);

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
            string endpoint = Settings.IsLazerVersion ? "releases" : "releases/latest";
            OS.ShellOpen($"https://github.com/{Settings.GITHUB_REPO_PATH}/{endpoint}");
            return;
        }

        Process.Start(Settings.AutoUpdateInstallerPath, "/silent");
    }
}
