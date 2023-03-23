using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OsuSkinMixer.Components;

public partial class SkinComponentsContainer : PanelContainer
{
    public event Action<OsuSkin> SkinSelected;

    public event Action<OsuSkin, bool> SkinChecked;

    public Action<IEnumerable<OsuSkin>> SkinInfoRequested
    {
        get => ManageSkinPopup.SkinInfoRequested;
        set => ManageSkinPopup.SkinInfoRequested = value;
    }

    public bool CheckableComponents { get; set; }

    public SkinComponent BestMatch => VBoxContainer.GetChildren().Cast<SkinComponent>().FirstOrDefault(c => c.Visible);

    public IEnumerable<SkinComponent> VisibleComponents => VBoxContainer.GetChildren().Cast<SkinComponent>().Where(c => c.Visible);

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
            VBoxContainer.AddChild(CreateSkinComponentFrom(skin));
    }

    public void FilterSkins(string filter)
    {
        string[] filterWords = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var component in VBoxContainer.GetChildren().Cast<SkinComponent>())
        {
            bool filterMatch = filterWords.All(w => component.Name.ToString().Contains(w, StringComparison.OrdinalIgnoreCase));
            bool visible = filterMatch && !_disabledSkinComponents.Contains(component);

            component.Visible = visible;

            if (component.IsChecked && !visible)
                component.IsChecked = visible;
        }
    }

    public void SortSkins(SkinSort sort)
    {
        _sort = sort;

        IEnumerable<SkinComponent> children = VBoxContainer.GetChildren().Cast<SkinComponent>().OrderBy(c => c.Name.ToString());

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
            VBoxContainer.MoveChild(child, Array.IndexOf(sortedArray, child));
    }

    public void DisableSkinComponent(OsuSkin skin)
    {
        var skinComponent = GetExistingComponentFromSkin(skin);

        // Skin is no longer available, maybe it was deleted.
        if (skinComponent == null)
            return;

        skinComponent.Visible = false;
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

    public void SelectAll(bool select)
    {
        foreach (var component in VBoxContainer.GetChildren().Cast<SkinComponent>().Where(c => c.Visible))
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
        return VBoxContainer.GetChildren().Cast<SkinComponent>().FirstOrDefault(c => c.Skin.Name == skin.Name);
    }

    private void OnSkinAdded(OsuSkin skin)
    {
        var skinComponent = CreateSkinComponentFrom(skin);
        VBoxContainer.AddChild(skinComponent);
        SortSkins(_sort);
    }

    private void OnSkinModified(OsuSkin skin)
    {
        var skinComponent = GetExistingComponentFromSkin(skin);
        skinComponent.Skin = skin;
        skinComponent.SetValues();

        if (_sort == SkinSort.Hidden || _sort == SkinSort.LastModified)
            SortSkins(_sort);
    }

    private void OnSkinRemoved(OsuSkin skin)
    {
        var skinComponent = GetExistingComponentFromSkin(skin);
        skinComponent.IsChecked = false;
        skinComponent.QueueFree();
    }
}
