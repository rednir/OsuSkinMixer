namespace OsuSkinMixer.Components;

using Environment = System.Environment;
using OsuSkinMixer.Statics;
using System.Threading.Tasks;
using System.IO;

public partial class SetupPopup : Popup
{
    protected override bool IsImportant => true;

    private LineEdit LineEdit;
    private Button DoneButton;
    private Button FolderPickerButton;
    private FileDialog FileDialog;
    private OkPopup OkPopup;
    private LazerWarningPopup LazerWarningPopup;
    private AnimationPlayer SpinnerAnimationPlayer;

    public override void _Ready()
    {
        base._Ready();

        LineEdit = GetNode<LineEdit>("%LineEdit");
        DoneButton = GetNode<Button>("%DoneButton");
        FolderPickerButton = GetNode<Button>("%FolderPickerButton");
        FileDialog = GetNode<FileDialog>("%FileDialog");
        OkPopup = GetNode<OkPopup>("%OkPopup");
        LazerWarningPopup = GetNode<LazerWarningPopup>("%LazerWarningPopup");
        SpinnerAnimationPlayer = GetNode<AnimationPlayer>("%SpinnerAnimationPlayer");

        DoneButton.Pressed += DoneButtonPressed;
        FolderPickerButton.Pressed += FolderPickerButtonPressed;
        FileDialog.DirSelected += d => LineEdit.Text = d;
    }

    public override void In()
    {
        base.In();
        LineEdit.Text = Settings.Content.OsuFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
    }

    public Task<bool> ShowLazerWarningAsync(bool schemaMismatch = false) => LazerWarningPopup.ConfirmAsync(schemaMismatch);

    public void ShowLoadingState()
    {
        DoneButton.Visible = false;
        SpinnerAnimationPlayer.Play("spin");
    }

    private async void DoneButtonPressed()
    {
        DoneButton.Visible = false;
        string path = LineEdit.Text.Trim('"').Trim('\'');
        LineEdit.Text = path;

        if (File.Exists(Path.Combine(path, "client.realm")))
        {
            bool schemaMismatch = false;
            SpinnerAnimationPlayer.Play("spin");
            try
            {
                schemaMismatch = await Task.Run(() =>
                {
                    try
                    {
                        var library = new Storage.LazerSkinLibrary(path, Path.Combine(Settings.AppdataFolderPath, "realm-backups"));
                        library.InspectCompatibility();
                        return library.WriteRestriction != null;
                    }
                    catch
                    {
                        // The normal folder validation below will present an actionable error.
                        return false;
                    }
                });
            }
            finally
            {
                SpinnerAnimationPlayer.Play("stop");
            }

            if (!await LazerWarningPopup.ConfirmAsync(schemaMismatch, includeNormalWarning: true))
            {
                DoneButton.Visible = true;
                return;
            }
        }

        SpinnerAnimationPlayer.Play("spin");
        try
        {
            var result = await Task.Run(() =>
            {
                bool success = Settings.TrySetOsuFolder(path, out string error);
                return (success, error);
            });

            if (!result.success)
            {
                OkPopup.SetValues(result.error, "That doesn't seem right...");
                OkPopup.In();
                return;
            }

            if (OsuData.Library is Storage.LazerSkinLibrary lazer && lazer.WriteRestriction != null)
                lazer.AllowUnsupportedSchemaWrites();
            Settings.Save();
            Out();
        }
        finally
        {
            SpinnerAnimationPlayer.Play("stop");
            DoneButton.Visible = true;
        }
    }

    private void FolderPickerButtonPressed()
    {
        FileDialog.CurrentDir = LineEdit.Text;
        FileDialog.PopupCentered();
    }
}
