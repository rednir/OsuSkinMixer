namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

public partial class SkinSelectorPopup : Popup
{
    public Action<OsuSkin> OnSelected { get; set; }

    public bool ShowPreviewButtons { get; set; }

    public SkinComponentsContainer SkinComponentsContainer => _skinComponentsContainer;

    private bool _initialised;

    private bool _isCompact;

    private bool? _showPreviewButtons = null;
    
    private Button BackButton;
    private LineEdit SearchLineEdit;
    private VBoxContainer SkinOptionsContainer;
    private SkinComponentsContainer _skinComponentsContainer;

    public override void _Ready()
    {
        base._Ready();

        BackButton = GetNode<Button>("%BackButton");
        _skinComponentsContainer = GetNode<SkinComponentsContainer>("%SkinComponentsContainer");
        SkinOptionsContainer = GetNode<VBoxContainer>("%SkinOptionsContainer");
        SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");

        BackButton.Pressed += Out;
        _skinComponentsContainer.ManageSkinOptions = ManageSkinOptions.All & ~ManageSkinOptions.Modify & ~ManageSkinOptions.Rename;
        _skinComponentsContainer.SkinInfoRequested = null;
        _skinComponentsContainer.SkinSelected += OnSkinSelected;
        SearchLineEdit.TextChanged += OnSearchTextChanged;
        SearchLineEdit.TextSubmitted += _ => OnSkinSelected(_skinComponentsContainer.BestMatch?.Skin);

        OsuData.SkinInfoRequested += OnSkinInfoRequested;
    }

    public override void _ExitTree()
    {
        OsuData.SkinInfoRequested -= OnSkinInfoRequested;
    }

    public override void In()
    {
        base.In();

        if (!_initialised)
        {
            SetCompactFlag();
            _skinComponentsContainer.InitialiseSkinComponents();
            _initialised = true;
        }
        else if (Settings.Content.UseCompactSkinSelector != _isCompact)
        {
            SetCompactFlag();
        }

        if (ShowPreviewButtons != _showPreviewButtons)
        {
            _showPreviewButtons = ShowPreviewButtons;
            _skinComponentsContainer.SetPreviewButtonVisibility(_showPreviewButtons ?? false);
        }

        SearchLineEdit.GrabFocus();
    }

    public void DisableSkinComponent(OsuSkin skin)
        => _skinComponentsContainer.DisableSkinComponent(skin);

    public void EnableSkinComponent(OsuSkin skin)
        => _skinComponentsContainer.EnableSkinComponent(skin);

    private void SetCompactFlag()
    {
        _isCompact = Settings.Content.UseCompactSkinSelector;
        _skinComponentsContainer.SkinComponentScene = _isCompact
            ? GD.Load<PackedScene>("res://src/Components/SkinComponentCompact.tscn")
            : GD.Load<PackedScene>("res://src/Components/SkinComponentSkinManager.tscn");
    }

    private void OnSkinInfoRequested(IEnumerable<OsuSkin> _)
    {
        Out();
    }

    private void OnSkinSelected(OsuSkin skin)
    {
        if (skin == null)
            return;

        OnSelected(skin);
    }

    private void OnSearchTextChanged(string text)
    {
        SkinOptionsContainer.Visible = string.IsNullOrWhiteSpace(text);
        _skinComponentsContainer.FilterSkins(text);
    }
}
