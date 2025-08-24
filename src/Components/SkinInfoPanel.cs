namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

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
    private Label LastModifiedLabel;
    private Button ModifyButton;
    private Button OpenFolderButton;
    private Button OpenInOsuButton;
    private Button ViewMoreButton;
    private PanelContainer ViewMorePadding;
    private VBoxContainer SkinCreditsContainer;
    private AudioStreamPlayer MenuHitPlayer;
    private ManageSkinPopup ManageSkinPopup;

    private Action _undoAction;

    private bool _isSkinCreditsInitialised;

    private PackedScene SkinComponentSkinCreditsScene = GD.Load<PackedScene>("res://src/Components/SkinComponentSkinCredits.tscn");

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
        LastModifiedLabel = GetNode<Label>("%LastModifiedLabel");
        ModifyButton = GetNode<Button>("%ModifyButton");
        OpenFolderButton = GetNode<Button>("%OpenFolderButton");
        OpenInOsuButton = GetNode<Button>("%OpenInOsuButton");
        ViewMoreButton = GetNode<Button>("%ViewMoreButton");
        ViewMorePadding = GetNode<PanelContainer>("%ViewMorePadding");
        SkinCreditsContainer = GetNode<VBoxContainer>("%SkinCreditsContainer");
        MenuHitPlayer = GetNode<AudioStreamPlayer>("%MenuHitPlayer");
        ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

        UndoDeleteButton.Pressed += OnUndoDeleteButtonPressed;
        MoreButton.Pressed += OnMoreButtonPressed;
        ModifyButton.Pressed += OnModifyButtonPressed;
        OpenFolderButton.Pressed += OnOpenFolderButtonPressed;
        OpenInOsuButton.Pressed += OnOpenInOsuButtonPressed;
        ViewMoreButton.Pressed += OnViewMoreButtonPressed;
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
        LastModifiedLabel.Text = $"Last modified: {(DateTime.Now - Skin.Directory.LastWriteTime).Humanise()}";
        OpenInOsuButton.Disabled = Skin.Hidden;
        MenuHitPlayer.Stream = Skin.GetAudioStream("menuhit");
        ManageSkinPopup.SetSkin(Skin);
    }

    private void OnSkinAdded(OsuSkin skin)
    {
        if (skin != Skin)
            return;

        MainContentContainer.Visible = true;
        DeletedContainer.Visible = false;
        CallDeferred(MethodName.SetValues);
    }

    private void OnSkinModified(OsuSkin skin)
    {
        if (skin != Skin)
            return;

        CallDeferred(MethodName.SetValues);
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
        MenuHitPlayer.Play();

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

    private void OnViewMoreButtonPressed()
    {
        ViewMorePadding.Visible = !ViewMorePadding.Visible;

        if (!_isSkinCreditsInitialised)
        {
            foreach (var credit in Skin.Credits.GetKeyValuePairs())
            {
                SkinComponent creditComponent = SkinComponentSkinCreditsScene.Instantiate<SkinComponent>();
                creditComponent.Skin = new OsuSkin(credit.Key.SkinName, credit.Key.SkinAuthor);
                SkinCreditsContainer.AddChild(creditComponent);
            }

            _isSkinCreditsInitialised = true;
        }
    }

    private void OnModifyButtonPressed()
    {
        OsuData.RequestSkinModify(new OsuSkin[] { Skin });
    }
}
