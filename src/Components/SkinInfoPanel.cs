namespace OsuSkinMixer.Components;

using System.IO;
using System.Text;
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

    private bool _isSkinCreditsInitialised;

    private PackedScene SkinComponentSkinCreditsScene = GD.Load<PackedScene>("res://src/Components/SkinComponentSkinCredits.tscn");

    private DpiTexture ExpandMoreIcon;
    private DpiTexture ExpandLessIcon;

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

        ExpandMoreIcon = GD.Load<DpiTexture>("res://assets/materialicons/expand_more.svg");
        ExpandLessIcon = GD.Load<DpiTexture>("res://assets/materialicons/expand_less.svg");

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
        LastModifiedLabel.Text = Skin.Record?.Problem ?? $"Last modified: {(DateTime.Now - Skin.Modified).Humanise()}";
        OpenInOsuButton.Disabled = Skin.IsLazer || Skin.Hidden || !Skin.CanExport;
        OpenInOsuButton.TooltipText = Skin.IsLazer ? "Feature not available for lazer" : string.Empty;
        OpenFolderButton.Visible = !Skin.IsLazer;
        ModifyButton.Disabled = !Skin.CanEdit;
        MenuHitPlayer.Stream = Skin.GetAudioStream("menuhit");
        InitialiseCreditsContainer();
    }

    private void OnSkinAdded(OsuSkin skin)
    {
        if (skin != Skin && skin.Credits.GetKeyValuePairs().Any(c => c.Key.SkinName == Skin.Name))
            return;

        if (skin == Skin)
        {
            MainContentContainer.Visible = true;
            DeletedContainer.Visible = false;
        }

        CallDeferred(MethodName.SetValues);
    }

    private void OnSkinModified(OsuSkin skin)
    {
        if (skin != Skin && skin.Credits.GetKeyValuePairs().Any(c => c.Key.SkinName == Skin.Name))
            return;

        CallDeferred(MethodName.SetValues);
    }

    private void OnSkinRemoved(OsuSkin skin)
    {
        if (skin != Skin)
        {
            if (Skin.Credits.GetKeyValuePairs().Any(c => c.Key.SkinName == skin.Name))
                CallDeferred(MethodName.SetValues);

            return;
        }

        MainContentContainer.Visible = false;
        DeletedContainer.Visible = true;

        UndoDeleteButton.Disabled = false;
    }

    private void OnUndoDeleteButtonPressed()
    {
        Settings.Content.Operations.LastOrDefault(o => o.Type == OperationType.Delete && o.TargetSkin?.Equals(Skin) == true)?.UndoOperation();
    }

    private void OnOpenFolderButtonPressed()
    {
        if (Skin.IsLazer) { Settings.PushToast("Export an .osk to edit this lazer skin externally."); return; }
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
        if (!ViewMorePadding.Visible)
            return;

        foreach (Node child in SkinCreditsContainer.GetChildren())
            child.QueueFree();

        List<SkinComponent> creditComponents = [];

        bool hasCredits = false;

        foreach (var credit in Skin.Credits.GetKeyValuePairs())
        {
            float creditPercentage = (float)credit.Value.Count / Skin.ElementCount * 100;

            SkinComponent creditComponent = SkinComponentSkinCreditsScene.Instantiate<SkinComponent>();
            creditComponent.CreditsElements = credit.Value.ToArray();
            creditComponent.CreditPercentage = (int)creditPercentage;
            creditComponent.RightClicked += () => OnCreditRightClicked(creditComponent);
            creditComponent.LeftClicked += () => OnCreditLeftClicked(creditComponent);
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
        ManageSkinPopup.Options = ManageSkinOptions.All;
        ManageSkinPopup.In();
    }

    private void OnCreditLeftClicked(SkinComponent creditComponent)
    {
        StringBuilder sb = new();
        foreach (var element in creditComponent.CreditsElements)
            sb.AppendLine($"• {element.Filename}");

        GetNode<OkPopup>("%SkinCreditsPopup").In();
        GetNode<Label>("%CreditedElementsLabel").Text = sb.ToString();
    }

    private void OnModifyButtonPressed()
    {
        ManageSkinPopup.SetSkin(Skin);
        ManageSkinPopup.Options |= ManageSkinOptions.All & ~ManageSkinOptions.OpenInOsu & ~ManageSkinOptions.OpenFolder;
        OsuData.RequestSkinModify(new OsuSkin[] { Skin });
    }
}
