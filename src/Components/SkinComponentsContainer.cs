namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

public partial class SkinComponentsContainer : PanelContainer
{
    public event Action<OsuSkin> SkinSelected;

    public event Action<OsuSkin, bool> SkinChecked;

    public Action<IEnumerable<OsuSkin>> SkinInfoRequested
    {
        get => ManageSkinPopup.SkinInfoRequested;
        set => ManageSkinPopup.SkinInfoRequested = value;
    }

    public ManageSkinOptions ManageSkinOptions
    {
        get => ManageSkinPopup.Options;
        set => ManageSkinPopup.Options = value;
    }

    public bool CheckableComponents { get; set; }

    public List<SkinComponent> SkinComponents = new();

    public SkinComponent BestMatch => VBoxContainer.GetChildren().Cast<SkinComponent>().FirstOrDefault(c => c.Visible);

    private readonly List<SkinComponent> _disabledSkinComponents = new();

    private bool _skinComponentsInitialised;

    public PackedScene SkinComponentScene
    {
        get => _skinComponentScene;
        set
        {
            _skinComponentScene = value;

            if (_skinComponentsInitialised)
                InitialiseSkinComponents();
        }
    }

    private SkinSort _sort = SkinSort.Name;

    private PackedScene _skinComponentScene;

    private VBoxContainer VBoxContainer;
    private ManageSkinPopup ManageSkinPopup;

    public override void _Ready()
    {
        VBoxContainer = GetNode<VBoxContainer>("%VBoxContainer");
        ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

        SkinChecked += (_, _) =>
        {
            if (_sort == SkinSort.Selected)
                SortSkins(_sort);
        };

        OsuData.SkinAdded += OnSkinAdded;
        OsuData.SkinModified += OnSkinModified;
        OsuData.SkinRemoved += OnSkinRemoved;

        TreeExiting += () =>
        {
            OsuData.SkinAdded -= OnSkinAdded;
            OsuData.SkinModified -= OnSkinModified;
            OsuData.SkinRemoved -= OnSkinRemoved;
        };
    }

    public void InitialiseSkinComponents()
    {
        _skinComponentsInitialised = true;

        foreach (var child in VBoxContainer.GetChildren())
            child.QueueFree();

        foreach (OsuSkin skin in OsuData.Skins)
            AddSkinComponent(skin);
    }

    public void FilterSkins(string filter)
    {
        string[] filterWords = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var component in SkinComponents)
        {
            bool filterMatch = filterWords.All(w =>
                component.Name.ToString().Contains(w, StringComparison.OrdinalIgnoreCase)
                || component.Skin.SkinIni.TryGetPropertyValue("General", "Author")?.Contains(w, StringComparison.OrdinalIgnoreCase) == true);
            bool visible = filterMatch && !_disabledSkinComponents.Contains(component);

            component.Visible = visible;

            if (component.IsChecked && !visible)
                component.IsChecked = visible;
        }
    }

    public void SortSkins(SkinSort sort)
    {
        _sort = sort;

        IEnumerable<SkinComponent> children = SkinComponents.OrderBy(c => c.Name.ToString());

        switch (sort)
        {
            case SkinSort.Author:
                children = children.OrderBy(c => c.Skin.SkinIni.TryGetPropertyValue("General", "Author"));
                break;
            case SkinSort.LastModified:
                children = children.OrderByDescending(c => c.Skin.Directory.LastWriteTime);
                break;
            case SkinSort.Hidden:
                children = children.OrderByDescending(c => c.Skin.Hidden);
                break;
            case SkinSort.Selected:
                children = children.OrderByDescending(c => c.IsChecked);
                break;
        }

        SkinComponent[] sortedArray = children.ToArray();
        foreach (var child in children)
            VBoxContainer.CallDeferred(MethodName.MoveChild, child, Array.IndexOf(sortedArray, child));
    }

    public void DisableSkinComponent(OsuSkin skin)
    {
        var skinComponent = GetExistingComponentFromSkin(skin);

        // Skin is no longer available, maybe it was deleted.
        if (skinComponent == null)
            return;

        skinComponent.SetDeferred(PropertyName.Visible, false);
        _disabledSkinComponents.Add(skinComponent);
    }

    public void EnableSkinComponent(OsuSkin skin)
    {
        var skinComponent = GetExistingComponentFromSkin(skin);

        // Skin is no longer available, maybe it was deleted.
        if (skinComponent == null)
            return;

        skinComponent.Visible = true;
        _disabledSkinComponents.Remove(skinComponent);
    }

    private void AddSkinComponent(OsuSkin skin)
    {
        var skinComponent = CreateSkinComponentFrom(skin);
        VBoxContainer.CallDeferred(MethodName.AddChild, skinComponent);
        SkinComponents.Add(skinComponent);
    }

    private void RemoveSkinComponent(OsuSkin skin)
    {
        var skinComponent = GetExistingComponentFromSkin(skin);
        skinComponent.IsChecked = false;
        skinComponent.QueueFree();
        SkinComponents.Remove(skinComponent);
    }

    public void SelectAll(bool select)
    {
        foreach (var component in SkinComponents.Where(c => c.Visible))
            component.IsChecked = select;
    }

    private SkinComponent CreateSkinComponentFrom(OsuSkin skin)
    {
        SkinComponent instance = SkinComponentScene.Instantiate<SkinComponent>();
        instance.Skin = skin;
        instance.Name = skin.Name;
        instance.CheckBoxVisible = CheckableComponents;
        instance.LeftClicked += () => SkinSelected(skin);
        instance.Checked += p => SkinChecked(skin, p);
        instance.RightClicked += () =>
        {
            ManageSkinPopup.SetSkin(skin);
            ManageSkinPopup.In();
        };

        return instance;
    }

    private SkinComponent GetExistingComponentFromSkin(OsuSkin skin)
    {
        return SkinComponents.FirstOrDefault(c => c.Skin.Name == skin.Name);
    }

    private void OnSkinAdded(OsuSkin skin)
    {
        if (!_skinComponentsInitialised)
            return;

        AddSkinComponent(skin);
        SortSkins(_sort);
    }

    private void OnSkinModified(OsuSkin skin)
    {
        if (!_skinComponentsInitialised)
            return;

        var skinComponent = GetExistingComponentFromSkin(skin);
        skinComponent.Skin = skin;
        skinComponent.SetValues();

        if (_sort == SkinSort.Hidden || _sort == SkinSort.LastModified)
            SortSkins(_sort);
    }

    private void OnSkinRemoved(OsuSkin skin)
    {
        if (!_skinComponentsInitialised)
            return;

        RemoveSkinComponent(skin);
    }
}
