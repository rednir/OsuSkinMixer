namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Statics;

public partial class SkinSelectorPopup : Popup
{
    public Action<OsuSkinBase> OnSelected { get; set; }

    private bool _initialised;

    private bool _isCompact;

    private Button BackButton;
    private LineEdit SearchLineEdit;
    private VBoxContainer SkinOptionsContainer;
    private SkinComponentsContainer SkinComponentsContainer;

    public override void _Ready()
    {
        base._Ready();

        BackButton = GetNode<Button>("%BackButton");
        SkinComponentsContainer = GetNode<SkinComponentsContainer>("%SkinComponentsContainer");
        SkinOptionsContainer = GetNode<VBoxContainer>("%SkinOptionsContainer");
        SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");

        BackButton.Pressed += Out;
        SkinComponentsContainer.ManageSkinOptions = ManageSkinOptions.All & ~ManageSkinOptions.Modify;
        SkinComponentsContainer.SkinInfoRequested = null;
        SkinComponentsContainer.SkinSelected += OnSkinSelected;
        SearchLineEdit.TextChanged += OnSearchTextChanged;
        SearchLineEdit.TextSubmitted += _ => OnSkinSelected(SkinComponentsContainer.BestMatch?.Skin);

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
            SkinComponentsContainer.InitialiseSkinComponents();
            _initialised = true;
        }
        else if (Settings.Content.UseCompactSkinSelector != _isCompact)
        {
            SetCompactFlag();
        }

        SearchLineEdit.GrabFocus();
    }

    public void DisableSkinComponent(OsuSkinBase skin)
        => SkinComponentsContainer.DisableSkinComponent(skin);

    public void EnableSkinComponent(OsuSkinBase skin)
        => SkinComponentsContainer.EnableSkinComponent(skin);

    private void SetCompactFlag()
    {
        _isCompact = Settings.Content.UseCompactSkinSelector;
        SkinComponentsContainer.SkinComponentScene = _isCompact
            ? GD.Load<PackedScene>("res://src/Components/SkinComponentCompact.tscn")
            : GD.Load<PackedScene>("res://src/Components/SkinComponentSkinManager.tscn");
    }

    private void OnSkinInfoRequested(IEnumerable<OsuSkinBase> _)
    {
        Out();
    }

    private void OnSkinSelected(OsuSkinBase skin)
    {
        if (skin == null)
            return;

        OnSelected(skin);
    }

    private void OnSearchTextChanged(string text)
    {
        SkinOptionsContainer.Visible = string.IsNullOrWhiteSpace(text);
        SkinComponentsContainer.FilterSkins(text);
    }
}
