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

    public override void _Ready()
    {
        base._Ready();

        LineEdit = GetNode<LineEdit>("%LineEdit");
        DoneButton = GetNode<Button>("%DoneButton");
        FolderPickerButton = GetNode<Button>("%FolderPickerButton");
        FileDialog = GetNode<FileDialog>("%FileDialog");
        OkPopup = GetNode<OkPopup>("%OkPopup");
        LazerWarningPopup = GetNode<LazerWarningPopup>("%LazerWarningPopup");

        DoneButton.Pressed += DoneButtonPressed;
        FolderPickerButton.Pressed += FolderPickerButtonPressed;
        FileDialog.DirSelected += d => LineEdit.Text = d;
    }

    public override void In()
    {
        base.In();
        LineEdit.Text = Settings.Content.OsuFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
    }

    public Task<bool> ShowLazerWarningAsync() => LazerWarningPopup.ConfirmAsync();

    private async void DoneButtonPressed()
    {
        DoneButton.Disabled = true;
        string path = LineEdit.Text.Trim('"').Trim('\'');
        LineEdit.Text = path;

        if (File.Exists(Path.Combine(path, "client.realm")) && !await LazerWarningPopup.ConfirmAsync())
        {
            DoneButton.Disabled = false;
            return;
        }

        var result = await Task.Run(() =>
        {
            bool success = Settings.TrySetOsuFolder(path, out string error);
            return (success, error);
        });

        if (!result.success)
        {
            DoneButton.Disabled = false;
            OkPopup.SetValues(result.error, "That doesn't seem right...");
            OkPopup.In();
            return;
        }

        Settings.Save();
        Out();

        DoneButton.Disabled = false;
    }

    private void FolderPickerButtonPressed()
    {
        FileDialog.CurrentDir = LineEdit.Text;
        FileDialog.PopupCentered();
    }
}
