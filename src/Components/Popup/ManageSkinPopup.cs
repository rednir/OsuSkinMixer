using System.IO;
using System.IO.Compression;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class ManageSkinPopup : Popup
{
    public Action<IEnumerable<OsuSkin>> SkinInfoRequested { get; set; } = OsuData.RequestSkinInfo;

    public ManageSkinOptions Options { get; set; } = ManageSkinOptions.All;

    private OsuSkin[] _skins;

    private QuestionPopup DeleteQuestionPopup;
    private SkinNamePopup SkinNamePopup;
    private LoadingPopup LoadingPopup;
    private Label TitleLabel;
    private Button OpenInOsuButton;
    private Button OpenFolderButton;
    private Button ModifyButton;
    private Button HideButton;
    private Button ExportButton;
    private Button DuplicateButton;
    private Button DeleteButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();

        DeleteQuestionPopup = GetNode<QuestionPopup>("%DeleteQuestionPopup");
        SkinNamePopup = GetNode<SkinNamePopup>("%SkinNamePopup");
        LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");
        TitleLabel = GetNode<Label>("%Title");
        OpenInOsuButton = GetNode<Button>("%OpenInOsuButton");
        OpenFolderButton = GetNode<Button>("%OpenFolderButton");
        ModifyButton = GetNode<Button>("%ModifyButton");
        HideButton = GetNode<Button>("%HideButton");
        ExportButton = GetNode<Button>("%ExportButton");
        DuplicateButton = GetNode<Button>("%DuplicateButton");
        DeleteButton = GetNode<Button>("%DeleteButton");

        DeleteQuestionPopup.ConfirmAction = OnDeleteConfirmed;
        SkinNamePopup.ConfirmAction = OnDuplicateSkinNameConfirmed;
        OpenInOsuButton.Pressed += OnOpenInOsuButtonPressed;
        OpenFolderButton.Pressed += OnOpenFolderButtonPressed;
        ModifyButton.Pressed += OnModifyButtonPressed;
        HideButton.Pressed += OnHideButtonPressed;
        ExportButton.Pressed += OnExportButtonPressed;
        DuplicateButton.Pressed += OnDuplicateButtonPressed;
        DeleteButton.Pressed += OnDeleteButtonPressed;
    }

    public override void In()
    {
        if (_skins.Length == 0)
            return;

        SetValues();
        base.In();
    }

    public void SetSkin(OsuSkin skin)
    {
        _skins = new OsuSkin[] { skin };
    }

    public void SetSkins(IEnumerable<OsuSkin> skins)
    {
        _skins = skins.ToArray();
    }

    private void SetValues()
    {
        OpenInOsuButton.Visible = (Options & ManageSkinOptions.OpenInOsu) == ManageSkinOptions.OpenInOsu && _skins.Length == 1;
        OpenFolderButton.Visible = (Options & ManageSkinOptions.OpenFolder) == ManageSkinOptions.OpenFolder && _skins.Length <= 4;
        ModifyButton.Visible = (Options & ManageSkinOptions.Modify) == ManageSkinOptions.Modify;
        HideButton.Visible = (Options & ManageSkinOptions.Hide) == ManageSkinOptions.Hide;
        ExportButton.Visible = (Options & ManageSkinOptions.Export) == ManageSkinOptions.Export;
        DuplicateButton.Visible = (Options & ManageSkinOptions.Duplicate) == ManageSkinOptions.Duplicate;
        DeleteButton.Visible = (Options & ManageSkinOptions.Delete) == ManageSkinOptions.Delete;

        if (_skins.Length == 1)
        {
            TitleLabel.Text = _skins[0].Name;
            HideButton.Text = _skins[0].Hidden ? "    Unhide from osu!" : "    Hide from osu!";
            return;
        }

        TitleLabel.Text = $"{_skins.Length} skins selected";
        HideButton.Text = "    Toggle hidden state";
    }

    public void OnOpenInOsuButtonPressed()
    {
        try
        {
            Tools.TriggerOskImport(_skins[0]);
        }
        catch (Exception ex)
        {
            Settings.PushException(ex);
        }
        finally
        {
            Out();
        }
    }

    public void OnOpenFolderButtonPressed()
    {
        Out();
        foreach (var skin in _skins)
            Tools.ShellOpenFile(skin.Directory.FullName);
    }

    public void OnModifyButtonPressed()
    {
        OsuData.RequestSkinModify(_skins);
        Out();
    }

    public void OnHideButtonPressed()
    {
        Directory.CreateDirectory(Settings.HiddenSkinsFolderPath);

        Task.Run(async () =>
        {
            foreach (var skin in _skins)
            {
                if (skin.Hidden)
                {
                    await new Operation(
                        type: OperationType.Unhide,
                        targetSkin: skin,
                        action: () =>
                        {
                            skin.Directory.MoveTo(Path.Combine(Settings.SkinsFolderPath, skin.Name));
                            skin.Hidden = false;
                        })
                        .RunOperation();
                }
                else
                {
                    await new Operation(
                        type: OperationType.Hide,
                        targetSkin: skin,
                        action: () =>
                        {
                            skin.Directory.MoveTo(Path.Combine(Settings.HiddenSkinsFolderPath, skin.Name));
                            skin.Hidden = true;
                        })
                        .RunOperation();
                }

                OsuData.InvokeSkinModified(skin);
            }
        })
        .ContinueWith(_ => Out());
    }

    public void OnExportButtonPressed()
    {
        string exportFolderPath = Path.Combine(Settings.Content.OsuFolder, "Exports");
        LoadingPopup.In();

        Directory.CreateDirectory(exportFolderPath);

        Task.Run(async () =>
        {
            foreach (var skin in _skins)
            {
                await new Operation(
                    type: OperationType.Export,
                    targetSkin: skin,
                    action: () =>
                    {
                        string destPath = Path.Combine(exportFolderPath, $"{skin.Name}.osk");
                        if (File.Exists(destPath))
                            File.Delete(destPath);

                        ZipFile.CreateFromDirectory(skin.Directory.FullName, destPath);
                        LoadingPopup.Progress += 100.0 / _skins.Length;
                    },
                    undoAction: () =>
                    {
                        if (File.Exists(Path.Combine(exportFolderPath, $"{skin.Name}.osk")))
                            File.Delete(Path.Combine(exportFolderPath, $"{skin.Name}.osk"));
                    })
                    .RunOperation();
            }
        })
        .ContinueWith(_ =>
        {
            LoadingPopup.Out();
            Out();
            Tools.ShellOpenFile(exportFolderPath);
        });
    }

    public void OnDuplicateButtonPressed()
    {
        if (_skins.Length > 1)
        {
            SkinNamePopup.LineEditText = " (copy)";
            SkinNamePopup.SkinNames = _skins.Select(s => s.Name).ToArray();
            SkinNamePopup.SuffixMode = true;
        }
        else
        {
            SkinNamePopup.LineEditText = $"{_skins[0].Name} (copy)";
            SkinNamePopup.SkinNames = new string[] { _skins[0].Name };
            SkinNamePopup.SuffixMode = false;
        }

        SkinNamePopup.In();
    }

    private void OnDuplicateSkinNameConfirmed(string value)
    {
        LoadingPopup.In();
        List<OsuSkin> newSkins = new();

        Task.Run(async () =>
        {
            foreach (OsuSkin skin in _skins)
            {
                OsuSkin newSkin = null;

                await new Operation(
                    type: OperationType.Duplicate,
                    targetSkin: skin,
                    action: () =>
                    {
                        newSkin = DuplicateSingleSkin(skin, SkinNamePopup.SuffixMode ? skin.Name + value : value);
                        newSkins.Add(newSkin);
                        LoadingPopup.Progress += 100.0 / _skins.Length;
                    },
                    undoAction: () =>
                    {
                        if (Directory.Exists(newSkin.Directory.FullName))
                        {
                            newSkin.Directory.Delete(true);
                            OsuData.RemoveSkin(newSkin);
                        }
                    })
                    .RunOperation();
            }

            SkinInfoRequested?.Invoke(newSkins);
        })
        .ContinueWith(_ =>
        {
            LoadingPopup.Out();
            SkinNamePopup.Out();
            Out();
        });
    }

    private static OsuSkin DuplicateSingleSkin(OsuSkin skin, string newSkinName)
    {
        OsuSkin newSkin = new(skin.Directory.CopyDirectory(Path.Combine(Settings.SkinsFolderPath, newSkinName), true));
        OsuData.AddSkin(newSkin);
        return newSkin;
    }

    public void OnDeleteButtonPressed()
    {
        DeleteQuestionPopup.In();
    }

    private void OnDeleteConfirmed()
    {
        LoadingPopup.In();
        Directory.CreateDirectory(Settings.TrashFolderPath);

        Task.Run(async () =>
        {
            foreach (OsuSkin skin in _skins)
            {
                await new Operation(
                    type: OperationType.Delete,
                    targetSkin: skin,
                    action: () =>
                    {
                        string trashPath = Path.Combine(Settings.TrashFolderPath, skin.Directory.Name);

                        if (Directory.Exists(trashPath))
                            Directory.Delete(trashPath, true);

                        skin.Directory.MoveTo(trashPath);
                        OsuData.RemoveSkin(skin);
                        LoadingPopup.Progress += 100.0 / _skins.Length;
                    },
                    undoAction: () =>
                    {
                        if (!Directory.Exists(skin.Directory.FullName))
                            return;

                        string originalPath = Path.Combine(skin.Hidden ? Settings.HiddenSkinsFolderPath : Settings.SkinsFolderPath, skin.Directory.Name);
                        skin.Directory.MoveTo(originalPath);
                        OsuData.AddSkin(skin);
                    })
                    .RunOperation();
            }
        })
        .ContinueWith(_ =>
        {
            LoadingPopup.Out();
            Out();
        });
    }
}
