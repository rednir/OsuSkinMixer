using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace OsuSkinMixer.Components;

public partial class ManageSkinPopup : Popup
{
    private QuestionPopup DeleteQuestionPopup;
    private SkinNamePopup SkinNamePopup;
    private LoadingPopup LoadingPopup;
    private Label TitleLabel;
    private Button ModifyButton;
    private Button HideButton;
    private Button ExportButton;
    private Button DuplicateButton;
    private Button DeleteButton;

    private OsuSkin[] _skins;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();

        DeleteQuestionPopup = GetNode<QuestionPopup>("%DeleteQuestionPopup");
        SkinNamePopup = GetNode<SkinNamePopup>("%SkinNamePopup");
        LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");
        TitleLabel = GetNode<Label>("%Title");
        ModifyButton = GetNode<Button>("%ModifyButton");
        HideButton = GetNode<Button>("%HideButton");
        ExportButton = GetNode<Button>("%ExportButton");
        DuplicateButton = GetNode<Button>("%DuplicateButton");
        DeleteButton = GetNode<Button>("%DeleteButton");

        DeleteQuestionPopup.ConfirmAction = OnDeleteConfirmed;
        SkinNamePopup.ConfirmAction = OnDuplicateSkinNameConfirmed;
        ModifyButton.Pressed += OnModifyButtonPressed;
        HideButton.Pressed += OnHideButtonPressed;
        ExportButton.Pressed += OnExportButtonPressed;
        DuplicateButton.Pressed += OnDuplicateButtonPressed;
        DeleteButton.Pressed += OnDeleteButtonPressed;
    }

    public void SetSkin(OsuSkin skin)
    {
        _skins = new OsuSkin[] { skin };
        TitleLabel.Text = skin.Name;
        HideButton.Text = skin.Hidden ? "    Unhide from osu!" : "    Hide from osu!";
    }

    public void SetSkins(IEnumerable<OsuSkin> skins)
    {
        _skins = skins.ToArray();
        TitleLabel.Text = $"{_skins.Length} skins selected";
        HideButton.Text = "    Toggle hidden state";
    }

    private void OnModifyButtonPressed()
    {
        OsuData.RequestSkinModify(_skins);
        Out();
    }

    private void OnHideButtonPressed()
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

    private void OnExportButtonPressed()
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

                        ZipFile.CreateFromDirectory(Path.Combine(Settings.SkinsFolderPath, skin.Name), destPath);
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

    private void OnDuplicateButtonPressed()
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

            OsuData.RequestSkinInfo(newSkins);
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
        Settings.Log($"Duplicating skin: {skin.Name} -> {newSkinName}");
        OsuSkin newSkin = new(skin.Directory.CopyDirectory(Path.Combine(Settings.SkinsFolderPath, newSkinName), true));
        OsuData.AddSkin(newSkin);
        return newSkin;
    }

    private void OnDeleteButtonPressed()
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
