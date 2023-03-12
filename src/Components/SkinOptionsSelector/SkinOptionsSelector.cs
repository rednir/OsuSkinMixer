using Godot;
using System.Collections.Generic;
using System.Linq;
using System;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SkinOptionsSelector : VBoxContainer
{
    public SkinOption[] SkinOptions { get; } = SkinOption.Default;

    private PackedScene SkinOptionComponentScene;

    private Panel ExpandHint;
    private SkinSelectorPopup SkinSelectorPopup;

    private SkinOptionComponent SkinOptionComponentInSelection;

    private List<SkinOptionComponent> SkinOptionComponents;

    private readonly Random Random = new();

    public override void _Ready()
    {
        SkinOptionComponentScene = GD.Load<PackedScene>("res://src/Components/SkinOptionsSelector/SkinOptionComponent.tscn");

        ExpandHint = GetNode<Panel>("ExpandHint");
        SkinSelectorPopup = GetNode<SkinSelectorPopup>("SkinSelectorPopup");

        SkinSelectorPopup.OnSelected = s => OptionComponentSelected(new SkinOptionValue(s));
        ExpandHint.Visible = !Settings.Content.ArrowButtonPressed;
    }

    public void CreateOptionComponents(SkinOptionValue defaultValue)
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

            component.ResetButton.Pressed += () =>
            {
                SkinOptionComponentInSelection = component;
                OptionComponentSelected(defaultValue);
            };
            component.Button.Pressed += () =>
            {
                SkinOptionComponentInSelection = component;
                SkinSelectorPopup.In();
            };

            component.SetSkinOption(option, defaultValue, layer);

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

                component.ArrowButton.Toggled += p =>
                {
                    newVbox.Visible = p;

                    if (!Settings.Content.ArrowButtonPressed)
                    {
                        ExpandHint.Visible = false;
                        Settings.Content.ArrowButtonPressed = true;
                        Settings.Save();
                    }
                };

                foreach (var child in parentOption.Children)
                    addOptionComponent(child, newVbox, layer + 1);
            }

            SkinOptionComponents.Add(component);
        }
    }

    public void Randomize()
    {
        foreach (var component in SkinOptionComponents.Where(c => c.SkinOption is not ParentSkinOption))
        {
            SkinOptionComponentInSelection = component;
            OptionComponentSelected(new SkinOptionValue(OsuData.Skins[Random.Next(OsuData.Skins.Length)]));
        }
    }

    public void Reset()
    {
        foreach (var component in SkinOptionComponents.Where(c => c.SkinOption is not ParentSkinOption))
        {
            SkinOptionComponentInSelection = component;
            OptionComponentSelected(component.DefaultValue);
        }
    }

    public void OptionComponentSelected(SkinOptionValue valueSelected)
    {
        Settings.Log($"Skin option '{SkinOptionComponentInSelection.SkinOption.Name}' set to '{valueSelected}'");

        // TODO: This method can be optimized further by recursively looping through the components and their
        // children (in their respective VBoxContainers) instead of looping through the ParentSkinOption's children.
        SkinOptionComponentInSelection.SetValue(valueSelected);

        foreach (var parent in SkinOption.GetParents(SkinOptionComponentInSelection.SkinOption, SkinOptions))
        {
            SkinOptionComponent parentOptionComponent = SkinOptionComponents.Find(c => c.SkinOption == parent);
            if (parent.Children.All(o => o.Value == valueSelected))
                parentOptionComponent.SetValue(valueSelected);
            else
                parentOptionComponent.SetValue(new SkinOptionValue(SkinOptionValueType.Various));
        }

        SetValueOfAllChildrenOfOption(SkinOptionComponentInSelection.SkinOption, valueSelected);
        SkinSelectorPopup.Out();
        SkinOptionComponentInSelection.Button.GrabFocus();
    }

    private void SetValueOfAllChildrenOfOption(SkinOption option, SkinOptionValue value)
    {
        if (option is ParentSkinOption parentOption)
        {
            foreach (var child in parentOption.Children)
                SetValueOfAllChildrenOfOption(child, value);
        }

        SkinOptionComponents.Find(c => c.SkinOption == option).SetValue(value);
    }
}
