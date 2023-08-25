namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

public partial class SkinManager : StackScene
{
    public override string Title => "Skin Manager";

    private PackedScene SkinInfoScene;

    private LineEdit SearchLineEdit;
    private Button SelectAllButton;
    private Button DeselectAllButton;
    private Button ManageSkinButton;
    private SkinSortChipsContainer SkinSortChipsContainer;
    private SkinComponentsContainer SkinComponentsContainer;
    private ManageSkinPopup ManageSkinPopup;

    private readonly List<OsuSkin> _checkedSkins = new();

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

        SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");
        SelectAllButton = GetNode<Button>("%SelectAllButton");
        DeselectAllButton = GetNode<Button>("%DeselectAllButton");
        ManageSkinButton = GetNode<Button>("%ManageSkinButton");
        SkinSortChipsContainer = GetNode<SkinSortChipsContainer>("%SkinSortChipsContainer");
        SkinComponentsContainer = GetNode<SkinComponentsContainer>("%SkinComponentsContainer");
        ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

        SkinComponentsContainer.SkinComponentScene = Settings.Content.UseCompactSkinSelector
            ? GD.Load<PackedScene>("res://src/Components/SkinComponentCompact.tscn")
            : GD.Load<PackedScene>("res://src/Components/SkinComponentSkinManager.tscn");

        SkinComponentsContainer.CheckableComponents = true;
        SkinComponentsContainer.SkinSelected += OnSkinSelected;
        SkinComponentsContainer.SkinChecked += OnSkinChecked;
        SkinComponentsContainer.InitialiseSkinComponents();

        SearchLineEdit.TextChanged += OnSearchTextChanged;
        SelectAllButton.Pressed += () => SkinComponentsContainer.SelectAll(true);
        DeselectAllButton.Pressed += () => SkinComponentsContainer.SelectAll(false);
        ManageSkinButton.Pressed += OnManageSkinButtonPressed;
        SkinSortChipsContainer.SortSelected += SkinComponentsContainer.SortSkins;

        UpdateSelectAllButtons();
    }

    private void UpdateSelectAllButtons()
    {
        SelectAllButton.Disabled = _checkedSkins.Count == SkinComponentsContainer.SkinComponents.Where(c => c.Visible).Count();
        DeselectAllButton.Disabled = _checkedSkins.Count == 0;
    }

    private void OnSkinSelected(OsuSkin skin)
    {
        SkinInfo instance = SkinInfoScene.Instantiate<SkinInfo>();
        instance.Skins = new OsuSkin[] { skin };
        EmitSignal(SignalName.ScenePushed, instance);
    }

    private void OnSkinChecked(OsuSkin skin, bool isChecked)
    {
        if (isChecked)
            _checkedSkins.Add(skin);
        else
            _checkedSkins.Remove(skin);

        ManageSkinButton.Disabled = _checkedSkins.Count == 0;
        UpdateSelectAllButtons();
    }

    private void OnSearchTextChanged(string newText)
    {
        SkinComponentsContainer.FilterSkins(newText);
        UpdateSelectAllButtons();
    }

    private void OnManageSkinButtonPressed()
    {
        ManageSkinPopup.SetSkins(_checkedSkins);
        ManageSkinPopup.In();
    }
}
