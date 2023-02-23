using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Components.SkinSelector;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.Components.SkinOptionsSelector;

public partial class SkinOptionsSelector : VBoxContainer
{
    public SkinOption[] SkinOptions { get; } = SkinOption.Default;

    private PackedScene SkinOptionComponentScene;

    private SkinSelectorPopup SkinSelectorPopup;

    private SkinOptionComponent SkinOptionComponentInSelection;

    private List<SkinOptionComponent> SkinOptionComponents;

    public override void _Ready()
    {
        SkinOptionComponentScene = GD.Load<PackedScene>("res://src/Components/SkinOptionsSelector/SkinOptionComponent.tscn");

        SkinSelectorPopup = GetNode<SkinSelectorPopup>("SkinSelectorPopup");
        SkinSelectorPopup.CreateSkinComponents(s => OptionComponentSelected(s));

        CreateOptionComponents();
    }

    public void CreateOptionComponents()
    {
        foreach (Node child in GetChildren())
            (child as SkinOptionComponent)?.QueueFree();

        SkinOptionComponents = new List<SkinOptionComponent>();

        foreach (var option in SkinOptions)
            addOptionComponent(option, this, 0);

        void addOptionComponent(SkinOption option, VBoxContainer vbox, int layer)
        {
            SkinOptionComponent component = SkinOptionComponentScene.Instantiate<SkinOptionComponent>();
            vbox.AddChild(component);
            component.SetValues(option, layer);

            component.Button.Pressed += () =>
            {
                SkinOptionComponentInSelection = component;
                SkinSelectorPopup.In();
            };

            if (option is ParentSkinOption parentOption)
            {
                var newVbox = new VBoxContainer()
                {
                    CustomMinimumSize = new Vector2(10, 0),
                    Visible = false,
                };
                newVbox.AddThemeConstantOverride("separation", 8);

                vbox.AddChild(newVbox);
                vbox.MoveChild(newVbox, component.GetIndex() + 1);

                component.ArrowButton.Toggled += p => newVbox.Visible = p;

                foreach (var child in parentOption.Children)
                    addOptionComponent(child, newVbox, layer + 1);
            }

            SkinOptionComponents.Add(component);
        }
    }

    private void OptionComponentSelected(Skin skinSelected)
    {
        // TODO: This method can be optimized further by recursively looping through the components and their
        // children (in their respective VBoxContainers) instead of looping through the ParentSkinOption's children.
        SkinOptionComponentInSelection.SetSelectedSkin(skinSelected);

        foreach (var parent in SkinOption.GetParents(SkinOptionComponentInSelection.SkinOption, SkinOptions))
        {
            SkinOptionComponent parentOptionComponent = SkinOptionComponents.Find(c => c.SkinOption == parent);
            if (parent.Children.All(o => o.Skin == skinSelected))
                parentOptionComponent.SetSelectedSkin(skinSelected);
            else
                parentOptionComponent.SetToVarious();
        }

        SetAllChildrenOfOptionToSkin(SkinOptionComponentInSelection.SkinOption, skinSelected);
    }

    private void SetAllChildrenOfOptionToSkin(SkinOption option, Skin skin)
    {
        if (option is ParentSkinOption parentOption)
        {
            foreach (var child in parentOption.Children)
                SetAllChildrenOfOptionToSkin(child, skin);
        }

        SkinOptionComponents.Find(c => c.SkinOption == option).SetSelectedSkin(skin);
    }
}
