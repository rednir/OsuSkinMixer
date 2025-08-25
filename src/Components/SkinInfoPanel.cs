namespace OsuSkinMixer.Components;

using System.IO;
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
    private Label SkinCreditsLabel;
    private VBoxContainer SkinCreditsContainer;
    private AudioStreamPlayer MenuHitPlayer;
    private ManageSkinPopup ManageSkinPopup;

    private Action _undoAction;
    private bool _isSkinCreditsInitialised;

    private PackedScene SkinComponentSkinCreditsScene = GD.Load<PackedScene>("res://src/Components/SkinComponentSkinCredits.tscn");

    private Texture2D ExpandMoreIcon;
    private Texture2D ExpandLessIcon;

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
        SkinCreditsLabel = GetNode<Label>("%SkinCreditsLabel");
        SkinCreditsContainer = GetNode<VBoxContainer>("%SkinCreditsContainer");
        MenuHitPlayer = GetNode<AudioStreamPlayer>("%MenuHitPlayer");
        ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

        ExpandMoreIcon = GD.Load<Texture2D>("res://assets/materialicons/expand_more.png");
        ExpandLessIcon = GD.Load<Texture2D>("res://assets/materialicons/expand_less.png");

        UndoDeleteButton.Pressed += OnUndoDeleteButtonPressed;
        MoreButton.Pressed += OnMoreButtonPressed;
        ModifyButton.Pressed += OnModifyButtonPressed;
        OpenFolderButton.Pressed += OnOpenFolderButtonPressed;
        OpenInOsuButton.Pressed += OnOpenInOsuButtonPressed;
        ViewMoreButton.Pressed += OnViewMoreButtonPressed;

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
        ManageSkinPopup.SetSkin(Skin);
        ManageSkinPopup.In();
    }

    private void OnViewMoreButtonPressed()
    {
        ViewMorePadding.Visible = !ViewMorePadding.Visible;
        ViewMoreButton.Icon = ViewMorePadding.Visible ? ExpandLessIcon : ExpandMoreIcon;

        if (!_isSkinCreditsInitialised)
            InitialiseCreditsContainer();
    }

    private void InitialiseCreditsContainer()
    {
        List<SkinComponent> creditComponents = [];

        bool hasCredits = false;

        foreach (var credit in Skin.Credits.GetKeyValuePairs())
        {
            float creditPercentage = (float)credit.Value.Count / Skin.ElementCount * 100;

            SkinComponent creditComponent = SkinComponentSkinCreditsScene.Instantiate<SkinComponent>();
            creditComponent.CreditPercentage = (int)creditPercentage;
            creditComponent.RightClicked += () => OnCreditRightClicked(creditComponent);
            creditComponent.Skin = OsuData.Skins.FirstOrDefault(s => s.Name == credit.Key.SkinName)
                ?? new OsuSkin(credit.Key.SkinName, credit.Key.SkinAuthor);

            creditComponents.Add(creditComponent);
            hasCredits = true;
        }

        if (!hasCredits)
        {
            SkinCreditsLabel.Text = "No credited skins.";
            SkinCreditsContainer.Visible = false;
            return;
        }

        foreach (SkinComponent component in creditComponents.OrderByDescending(c => c.CreditPercentage).ThenBy(c => c.Skin.Name))
            SkinCreditsContainer.AddChild(component);

        _isSkinCreditsInitialised = true;
    }

    private void OnCreditRightClicked(SkinComponent creditComponent)
    {
        ManageSkinPopup.SetSkin(creditComponent.Skin);
        // Haven't configured the skin credit to update on skin deleted, so be just disable deleting from here I guess. Although jank can still happen potentially.
        ManageSkinPopup.Options = ManageSkinOptions.All & ~ManageSkinOptions.Delete;
        ManageSkinPopup.In();
    }

    private void OnModifyButtonPressed()
    {
        ManageSkinPopup.SetSkin(Skin);
        ManageSkinPopup.Options |= ManageSkinOptions.All & ~ManageSkinOptions.OpenInOsu & ~ManageSkinOptions.OpenFolder;
        OsuData.RequestSkinModify(new OsuSkin[] { Skin });
    }
}
