using System;
using System.Linq;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SkinInfoPanel : PanelContainer
{
    public OsuSkin Skin { get; set; }

    private PanelContainer DeletedContainer;
    private Button UndoDeleteButton;
    private VBoxContainer MainContentContainer;
    private SkinPreview SkinPreview;
    private HitcircleIcon HitcircleIcon;
    private Label SkinNameLabel;
    private Label SkinAuthorLabel;
    private Button MoreButton;
    private Label DetailsLabel;
    private Button ModifyButton;
    private Button OpenFolderButton;
    private Button OpenInOsuButton;
    private ManageSkinPopup ManageSkinPopup;

    private Action _undoAction;

    public override void _Ready()
    {
        DeletedContainer = GetNode<PanelContainer>("%DeletedContainer");
        UndoDeleteButton = GetNode<Button>("%UndoDeleteButton");
        MainContentContainer = GetNode<VBoxContainer>("%MainContentContainer");
        SkinPreview = GetNode<SkinPreview>("%SkinPreview");
        HitcircleIcon = GetNode<HitcircleIcon>("%HitcircleIcon");
        SkinNameLabel = GetNode<Label>("%SkinName");
        SkinAuthorLabel = GetNode<Label>("%SkinAuthor");
        MoreButton = GetNode<Button>("%MoreButton");
        DetailsLabel = GetNode<Label>("%Details");
        ModifyButton = GetNode<Button>("%ModifyButton");
        OpenFolderButton = GetNode<Button>("%OpenFolderButton");
        OpenInOsuButton = GetNode<Button>("%OpenInOsuButton");
        ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

        UndoDeleteButton.Pressed += OnUndoDeleteButtonPressed;
        MoreButton.Pressed += OnMoreButtonPressed;
        ModifyButton.Pressed += OnModifyButtonPressed;
        OpenFolderButton.Pressed += OnOpenFolderButtonPressed;
        OpenInOsuButton.Pressed += OnOpenInOsuButtonPressed;
        ManageSkinPopup.Options = ManageSkinOptions.All & ~ManageSkinOptions.OpenInOsu & ~ManageSkinOptions.OpenFolder;

        OsuData.SkinAdded += OnSkinAdded;
        OsuData.SkinModified += OnSkinModified;
        OsuData.SkinRemoved += OnSkinRemoved;

        SetValues();
    }

    public override void _ExitTree()
    {
        OsuData.SkinAdded -= OnSkinAdded;
        OsuData.SkinModified -= OnSkinModified;
        OsuData.SkinRemoved -= OnSkinRemoved;
    }

    private void SetValues()
    {
        SkinPreview.SetSkin(Skin);
        HitcircleIcon.SetSkin(Skin);
        SkinNameLabel.Text = Skin.Name;
        SkinAuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
        DetailsLabel.Text = $"Last modified: {(DateTime.Now - Skin.Directory.LastWriteTime).Humanise()}";
        OpenInOsuButton.Disabled = Skin.Hidden;
        ManageSkinPopup.SetSkin(Skin);
    }

    private void OnSkinAdded(OsuSkin skin)
    {
        if (skin != Skin)
            return;

        MainContentContainer.Visible = true;
        DeletedContainer.Visible = false;
        SetValues();
    }

    private void OnSkinModified(OsuSkin skin)
    {
        if (skin != Skin)
            return;

        SetValues();
    }

    private void OnSkinRemoved(OsuSkin skin)
    {
        if (skin != Skin)
            return;

        MainContentContainer.Visible = false;
        DeletedContainer.Visible = true;

        Operation deleteOperation = Settings.Content.Operations.LastOrDefault(o => o.Type == OperationType.Delete && o.TargetSkin?.Name == skin.Name);
        if (deleteOperation?.CanUndo != true)
        {
            UndoDeleteButton.Disabled = true;
            return;
        }

        UndoDeleteButton.Disabled = false;
        _undoAction = deleteOperation.UndoOperation;
    }

    private void OnUndoDeleteButtonPressed()
    {
        _undoAction?.Invoke();
        UndoDeleteButton.Disabled = true;
    }

    private void OnOpenFolderButtonPressed()
    {
        Tools.ShellOpenFile(Skin.Directory.FullName);
    }

    private void OnOpenInOsuButtonPressed()
    {
        try
        {
            Tools.TriggerOskImport(Skin);
        }
        catch (Exception ex)
        {
            Settings.PushException(ex);
        }
    }

    private void OnMoreButtonPressed()
    {
        ManageSkinPopup.In();
    }

    private void OnModifyButtonPressed()
    {
        OsuData.RequestSkinModify(new OsuSkin[] { Skin });
    }
}
